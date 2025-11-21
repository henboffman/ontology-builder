using Eidos.Data.Repositories;
using Eidos.Models;
using Eidos.Services;
using Eidos.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics;
using Xunit;

namespace Eidos.Tests.Performance;

/// <summary>
/// Performance tests for ConceptDetectionService
/// Validates that detection completes in <2 seconds for 10K word notes
/// </summary>
public class ConceptDetectionPerformanceTests : IDisposable
{
    private readonly TestDbContextFactory _contextFactory;
    private readonly ConceptDetectionService _service;
    private readonly WorkspaceRepository _workspaceRepository;
    private readonly ConceptRepository _conceptRepository;
    private readonly ApplicationUser _testUser;

    public ConceptDetectionPerformanceTests()
    {
        var dbName = $"TestDb_{Guid.NewGuid()}";
        _contextFactory = new TestDbContextFactory(dbName);

        var workspaceLogger = new Mock<ILogger<WorkspaceRepository>>();
        _workspaceRepository = new WorkspaceRepository(_contextFactory, workspaceLogger.Object);
        _conceptRepository = new ConceptRepository(_contextFactory);

        var detectionLogger = new Mock<ILogger<ConceptDetectionService>>();
        _service = new ConceptDetectionService(
            _workspaceRepository,
            _conceptRepository,
            detectionLogger.Object);

        _testUser = TestDataBuilder.CreateUser();
    }

    public void Dispose()
    {
        // TestDbContextFactory doesn't implement IDisposable
        // In-memory database will be disposed when context is disposed
    }

    [Fact]
    public async Task DetectConceptsAsync_10KWordNote_CompletesUnder2Seconds()
    {
        // Arrange
        var (workspace, ontology) = await CreateWorkspaceWithOntologyAsync();

        // Create 50 concepts (realistic ontology size)
        var concepts = new List<Concept>();
        var conceptNames = new[]
        {
            "artificial intelligence", "machine learning", "deep learning", "neural network",
            "natural language processing", "computer vision", "reinforcement learning",
            "supervised learning", "unsupervised learning", "convolutional neural network",
            "recurrent neural network", "transformer", "attention mechanism", "backpropagation",
            "gradient descent", "optimization", "overfitting", "underfitting", "regularization",
            "dropout", "batch normalization", "activation function", "loss function",
            "training data", "validation data", "test data", "hyperparameter", "epoch",
            "batch size", "learning rate", "model architecture", "feature engineering",
            "dimensionality reduction", "principal component analysis", "clustering",
            "classification", "regression", "decision tree", "random forest", "support vector machine",
            "k-nearest neighbors", "ensemble learning", "boosting", "bagging", "cross-validation",
            "confusion matrix", "precision", "recall", "F1 score", "accuracy"
        };

        foreach (var name in conceptNames)
        {
            var concept = await CreateConceptAsync(ontology.Id, name);
            concepts.Add(concept);
        }

        // Generate 10K word note with concept mentions
        var content = GenerateLargeNote(10000, conceptNames);
        var noteId = 1;

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _service.DetectConceptsAsync(noteId, workspace.Id, content);
        stopwatch.Stop();

        // Assert - performance requirement: <2 seconds
        Assert.True(stopwatch.ElapsedMilliseconds < 2000,
            $"Detection took {stopwatch.ElapsedMilliseconds}ms, expected <2000ms");

        // Assert - accuracy: should detect at least some concepts
        Assert.NotEmpty(result);

        // Log results for analysis
        Console.WriteLine($"Performance Test Results:");
        Console.WriteLine($"  Note size: {CountWords(content)} words");
        Console.WriteLine($"  Concepts in ontology: {conceptNames.Length}");
        Console.WriteLine($"  Concepts detected: {result.Count}");
        Console.WriteLine($"  Total mentions: {result.Sum(r => r.TotalMentions)}");
        Console.WriteLine($"  Time taken: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"  Words per second: {CountWords(content) / (stopwatch.ElapsedMilliseconds / 1000.0):F0}");
    }

    [Fact]
    public async Task DetectConceptsAsync_5KWordNote_CompletesUnder1Second()
    {
        // Arrange
        var (workspace, ontology) = await CreateWorkspaceWithOntologyAsync();

        // Create 25 concepts
        var conceptNames = new[]
        {
            "artificial intelligence", "machine learning", "deep learning", "neural network",
            "natural language processing", "computer vision", "reinforcement learning",
            "supervised learning", "unsupervised learning", "convolutional neural network",
            "recurrent neural network", "transformer", "attention mechanism", "backpropagation",
            "gradient descent", "optimization", "overfitting", "underfitting", "regularization",
            "dropout", "batch normalization", "activation function", "loss function",
            "training data", "validation data"
        };

        foreach (var name in conceptNames)
        {
            await CreateConceptAsync(ontology.Id, name);
        }

        var content = GenerateLargeNote(5000, conceptNames);
        var noteId = 1;

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _service.DetectConceptsAsync(noteId, workspace.Id, content);
        stopwatch.Stop();

        // Assert - should complete even faster for smaller notes
        Assert.True(stopwatch.ElapsedMilliseconds < 1000,
            $"Detection took {stopwatch.ElapsedMilliseconds}ms, expected <1000ms for 5K words");

        Assert.NotEmpty(result);

        Console.WriteLine($"5K Word Note Performance:");
        Console.WriteLine($"  Time taken: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"  Concepts detected: {result.Count}");
    }

    [Fact]
    public async Task DetectConceptsAsync_20KWordNote_CompletesUnder4Seconds()
    {
        // Arrange - stress test with very large note
        var (workspace, ontology) = await CreateWorkspaceWithOntologyAsync();

        var conceptNames = new[]
        {
            "artificial intelligence", "machine learning", "deep learning", "neural network",
            "natural language processing", "computer vision", "reinforcement learning",
            "supervised learning", "unsupervised learning", "transformer"
        };

        foreach (var name in conceptNames)
        {
            await CreateConceptAsync(ontology.Id, name);
        }

        var content = GenerateLargeNote(20000, conceptNames);
        var noteId = 1;

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _service.DetectConceptsAsync(noteId, workspace.Id, content);
        stopwatch.Stop();

        // Assert - linear scaling: 20K words ≈ 4 seconds
        Assert.True(stopwatch.ElapsedMilliseconds < 4000,
            $"Detection took {stopwatch.ElapsedMilliseconds}ms, expected <4000ms for 20K words");

        Assert.NotEmpty(result);

        Console.WriteLine($"20K Word Note Performance (Stress Test):");
        Console.WriteLine($"  Time taken: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"  Concepts detected: {result.Count}");
    }

    [Fact]
    public async Task DetectConceptsAsync_ManyShortConcepts_EfficientProcessing()
    {
        // Arrange - test with many single-word concepts
        var (workspace, ontology) = await CreateWorkspaceWithOntologyAsync();

        var conceptNames = new[]
        {
            "AI", "ML", "DL", "NLP", "CV", "RL", "CNN", "RNN", "GAN", "LSTM",
            "GRU", "GPU", "CPU", "RAM", "API", "SDK", "IDE", "CLI", "GUI", "OS"
        };

        foreach (var name in conceptNames)
        {
            await CreateConceptAsync(ontology.Id, name);
        }

        var content = GenerateLargeNote(10000, conceptNames);
        var noteId = 1;

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _service.DetectConceptsAsync(noteId, workspace.Id, content);
        stopwatch.Stop();

        // Assert - short concepts should be even faster
        Assert.True(stopwatch.ElapsedMilliseconds < 1500,
            $"Detection took {stopwatch.ElapsedMilliseconds}ms, expected <1500ms for short concepts");

        Assert.NotEmpty(result);

        Console.WriteLine($"Many Short Concepts Performance:");
        Console.WriteLine($"  Time taken: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"  Concepts detected: {result.Count}");
    }

    // Helper Methods

    private string GenerateLargeNote(int targetWordCount, string[] conceptNames)
    {
        var random = new Random(42); // Fixed seed for reproducibility
        var paragraphs = new List<string>();

        var fillerWords = new[]
        {
            "the", "is", "a", "important", "very", "quite", "extremely", "somewhat",
            "field", "topic", "concept", "idea", "approach", "method", "technique",
            "application", "implementation", "development", "research", "study",
            "analysis", "process", "system", "model", "framework", "architecture",
            "in", "of", "for", "with", "by", "to", "from", "at", "on", "as"
        };

        int wordCount = 0;
        while (wordCount < targetWordCount)
        {
            var sentenceCount = random.Next(3, 8);
            var paragraph = new List<string>();

            for (int i = 0; i < sentenceCount && wordCount < targetWordCount; i++)
            {
                var sentence = new List<string>();
                var sentenceLength = random.Next(10, 25);

                for (int j = 0; j < sentenceLength && wordCount < targetWordCount; j++)
                {
                    // Mix concept mentions with filler words
                    if (random.Next(10) < 3 && conceptNames.Length > 0) // 30% chance of concept
                    {
                        var conceptName = conceptNames[random.Next(conceptNames.Length)];
                        sentence.Add(conceptName);
                        wordCount += conceptName.Split(' ').Length;
                    }
                    else
                    {
                        sentence.Add(fillerWords[random.Next(fillerWords.Length)]);
                        wordCount++;
                    }
                }

                if (sentence.Count > 0)
                {
                    sentence[0] = char.ToUpper(sentence[0][0]) + sentence[0].Substring(1);
                    paragraph.Add(string.Join(" ", sentence) + ".");
                }
            }

            if (paragraph.Count > 0)
            {
                paragraphs.Add(string.Join(" ", paragraph));
            }
        }

        return string.Join("\n\n", paragraphs);
    }

    private int CountWords(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        return content.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private async Task<(Workspace workspace, Ontology ontology)> CreateWorkspaceWithOntologyAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Create workspace first
        var workspace = new Workspace
        {
            Name = "Test Workspace",
            UserId = _testUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync();

        // Create ontology and link it to workspace
        // Note: WorkspaceId foreign key is on Ontology, not Workspace
        var ontology = new Ontology
        {
            Name = "Test Ontology",
            Description = "Test ontology for performance testing",
            UserId = _testUser.Id,
            WorkspaceId = workspace.Id,
            ConceptCount = 0,
            RelationshipCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Ontologies.Add(ontology);
        await context.SaveChangesAsync();

        return (workspace, ontology);
    }

    private async Task<Concept> CreateConceptAsync(int ontologyId, string name)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var concept = new Concept
        {
            Name = name,
            Definition = $"Definition of {name}",
            OntologyId = ontologyId,
            CreatedAt = DateTime.UtcNow
        };

        context.Concepts.Add(concept);
        await context.SaveChangesAsync();

        return concept;
    }
}
