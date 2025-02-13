using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;

class Program
{
    static void Main(string[] args)
    {
        // Étape 1 : Initialisation des événements
        List<string> customEvents = new List<string>
        {
            "Araignée",
            "Explosion",
            "Porte qui grince",
            "Musique joyeuse",
            "Course rapide"
        };

        EventSimulator simulator = new EventSimulator(customEvents, 600); // Jeu de 600 secondes
        List<(int Time, int HeartRate, string Event)> gameplayData = new List<(int, int, string)>();

        // Étape 2 : Collecte de données et entraînement ML
        int totalTime = 600; // Durée totale en secondes
        int trainingTime = (int)(totalTime * 0.2); // 20 % du temps pour l'entraînement

        for (int t = 0; t < totalTime; t++)
        {
            // Simuler une seconde de jeu
            var (time, heartRate, gameEvent) = simulator.SimulateSecond(t);
            gameplayData.Add((time, heartRate, gameEvent));

            // Afficher les données simulées
            Console.WriteLine($"Temps : {time}s, BPM : {heartRate}, Événement : {gameEvent}");

            // Entraîner le modèle après la période d'entraînement
            if (t == trainingTime)
            {
                Console.WriteLine("\n=== Entraînement du modèle ML ===\n");
                TrainAndAdaptGameplay(gameplayData, simulator, customEvents);
            }
        }

        Console.WriteLine("\nJeu terminé !");
    }

    // Fonction pour entraîner le modèle et adapter les événements
    static void TrainAndAdaptGameplay(
        List<(int Time, int HeartRate, string Event)> gameplayData,
        EventSimulator simulator,
        List<string> customEvents)
    {
        var context = new MLContext();

        // Préparation des données pour le modèle
        var eventData = gameplayData.Select(entry => new EventData
        {
            Event = entry.Event,
            HeartRate = entry.HeartRate,
            Time = (float)entry.Time // Conversion de 'Time' en float
        }).ToList();

        // Pipeline d'apprentissage : ajouter la normalisation et l'encodage OneHot
        var pipeline = context.Transforms.Conversion.MapValueToKey(nameof(EventData.Event))  // Encodage des événements en clés numériques
            .Append(context.Transforms.Categorical.OneHotEncoding(nameof(EventData.Event))) // Encodage One-Hot des événements
            .Append(context.Transforms.Concatenate("Features", nameof(EventData.Event), nameof(EventData.Time))) // Ajout du temps dans les caractéristiques
            .Append(context.Regression.Trainers.Sdca(
                labelColumnName: nameof(EventData.HeartRate), // Cible
                featureColumnName: "Features")); // Caractéristique

        var dataView = context.Data.LoadFromEnumerable(eventData);
        var model = pipeline.Fit(dataView);

        Console.WriteLine("\n=== Adaptation du gameplay ===");

        // Prédire les impacts des événements
        var predictionFunction = context.Model.CreatePredictionEngine<EventData, PredictionResult>(model);

        var eventImpacts = new Dictionary<string, List<float>>();
        foreach (var data in gameplayData)
        {
            var prediction = predictionFunction.Predict(new EventData { Event = data.Event, Time = (float)data.Time });
            Console.WriteLine($"Événement : {data.Event}, Impact prédit : {prediction.PredictedHeartRate:F2} BPM");

            // Ajouter une vérification pour voir si le modèle prédit toujours 0 ou des valeurs proches de 0
            if (Math.Abs(prediction.PredictedHeartRate) < 0.1)
            {
                Console.WriteLine("Attention : Le modèle prédit une valeur très faible, peut-être due à un apprentissage insuffisant.");
            }

            // Stocker les impacts pour calculer les moyennes
            if (!eventImpacts.ContainsKey(data.Event))
                eventImpacts[data.Event] = new List<float>();

            eventImpacts[data.Event].Add(prediction.PredictedHeartRate);
        }

        // Moyenne des impacts par événement
        Console.WriteLine("\n=== Analyse des impacts ===");
        foreach (var impact in eventImpacts)
        {
            float averageImpact = impact.Value.Average();
            Console.WriteLine($"Événement : {impact.Key}, Impact moyen : {averageImpact:F2} BPM");
        }

        // Adapter les événements du gameplay
        simulator.AdaptGameplayBasedOnImpacts(eventImpacts);
    }

    // Classe pour simuler les événements et les BPM
    public class EventSimulator
    {
        private readonly List<string> _events;
        private readonly Random _random;
        private readonly Dictionary<string, int> _eventWeights;

        public EventSimulator(List<string> events, int duration)
        {
            _events = events;
            _random = new Random();
            _eventWeights = _events.ToDictionary(e => e, e => 1); // Pondération initiale
        }

        public (int Time, int HeartRate, string Event) SimulateSecond(int time)
        {
            string selectedEvent = SelectRandomEvent();
            int heartRate = SimulateHeartRate(selectedEvent);
            return (time, heartRate, selectedEvent);
        }

        private string SelectRandomEvent()
        {
            int totalWeight = _eventWeights.Values.Sum();
            int randomValue = _random.Next(0, totalWeight);
            int cumulative = 0;

            foreach (var kvp in _eventWeights)
            {
                cumulative += kvp.Value;
                if (randomValue < cumulative)
                {
                    return kvp.Key;
                }
            }

            return _events[_random.Next(_events.Count)];
        }

        private int SimulateHeartRate(string gameEvent)
        {
            // Simuler une réponse physiologique basée sur l'événement
            return gameEvent switch
            {
                "Araignée" => _random.Next(85, 100),
                "Explosion" => _random.Next(100, 120),
                "Porte qui grince" => _random.Next(75, 85),
                "Musique joyeuse" => _random.Next(65, 75),
                "Course rapide" => _random.Next(90, 110),
                _ => _random.Next(70, 90),
            };
        }

        public void AdaptGameplayBasedOnImpacts(Dictionary<string, List<float>> eventImpacts)
        {
            foreach (var impact in eventImpacts)
            {
                float averageImpact = impact.Value.Average();
                AdaptEventPool(impact.Key, averageImpact);
            }
        }

        public void AdaptEventPool(string gameEvent, float predictedImpact)
        {
            // Modifier la pondération des événements en fonction de leur impact prédit
            if (predictedImpact > 90)
            {
                _eventWeights[gameEvent] += 2; // Augmenter la fréquence des événements effrayants
            }
            else
            {
                _eventWeights[gameEvent] = Math.Max(1, _eventWeights[gameEvent] - 1); // Diminuer la fréquence des événements moins impactants
            }
        }
    }

    // Classe des données d'entraînement
    public class EventData
    {
        public string Event { get; set; }
        public float HeartRate { get; set; }
        public float Time { get; set; } // Modifié en float pour correspondre au type des autres colonnes
    }

    // Classe pour la prédiction
    public class PredictionResult
    {
        public float PredictedHeartRate { get; set; }
    }
}
