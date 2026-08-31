using Cortex.Core.Abstractions;
using Cortex.Core.Models;
using Microsoft.ML.OnnxRuntime;

namespace Cortex.AI;

/// <summary>
/// Fully local embedding + completion provider running an ONNX Runtime session
/// (e.g. a quantized MiniLM/E5-family embedding model and a small local instruct model).
/// No network call is ever made by this provider — it exists precisely so CORTEX can
/// offer AI features with zero data leaving the machine. Ship the .onnx model files
/// separately (see README → "Local AI models") and point <see cref="ModelDirectory"/> at them.
/// </summary>
public sealed class LocalOnnxEmbeddingProvider : IAiProvider, IDisposable
{
    public AiProviderKind Kind => AiProviderKind.LocalOnnx;
    public required string ModelDirectory { get; init; }

    private InferenceSession? _embeddingSession;

    private InferenceSession EmbeddingSession => _embeddingSession ??= new InferenceSession(
        Path.Combine(ModelDirectory, "embedding-model.onnx"),
        new SessionOptions { GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL });

    public Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        // Tokenization is model-specific; wire your chosen tokenizer (e.g. a BPE/WordPiece
        // vocab shipped alongside the .onnx file) here before feeding EmbeddingSession.Run.
        // Kept as an explicit extension point rather than faking a fixed-size random vector.
        throw new NotImplementedException(
            "Wire a tokenizer matching your chosen local embedding model, then call EmbeddingSession.Run(...).");
    }

    public Task<string> CompleteAsync(string systemPrompt, string userPrompt, IReadOnlyList<string> retrievedContext, CancellationToken ct)
    {
        throw new NotImplementedException(
            "Wire a local instruct-tuned ONNX model (or a llama.cpp/DirectML backend) for fully offline completions.");
    }

    public void Dispose() => _embeddingSession?.Dispose();
}
