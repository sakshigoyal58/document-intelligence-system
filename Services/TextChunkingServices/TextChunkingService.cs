using Microsoft.ML.Tokenizers;

namespace Services.TextChunkingServices;

public class TextChunkingService : ITextChunkingService
{
    private const int SingleChunkTokenThreshold = 2000;
    private const int ChunkTokenSize = 650;
    private const int OverlapTokenSize = 65;

    private readonly Tokenizer _tokenizer;

    public TextChunkingService()
    {
        // cl100k_base — same encoding used by text-embedding-ada-002 / text-embedding-3-small/large
        _tokenizer = TiktokenTokenizer.CreateForModel("text-embedding-3-small");
    }

    public List<string> ChunkText(string text)
    {
        var totalTokens = _tokenizer.CountTokens(text);

        if (totalTokens <= SingleChunkTokenThreshold)
        {
            return new List<string> { text.Trim() };
        }

        var sentences = SplitIntoSentences(text);
        var chunks = new List<string>();
        var currentChunk = new List<string>();
        var currentTokenCount = 0;

        foreach (var sentence in sentences)
        {
            var sentenceTokenCount = _tokenizer.CountTokens(sentence);

            currentChunk.Add(sentence);
            currentTokenCount += sentenceTokenCount;

            if (currentTokenCount >= ChunkTokenSize)
            {
                var chunkText = string.Join(' ', currentChunk);
                chunks.Add(chunkText);

                // build overlap: keep trailing sentences worth ~OverlapTokenSize tokens
                var overlapChunk = new List<string>();
                var overlapTokens = 0;
                for (int i = currentChunk.Count - 1; i >= 0 && overlapTokens < OverlapTokenSize; i--)
                {
                    overlapChunk.Insert(0, currentChunk[i]);
                    overlapTokens += _tokenizer.CountTokens(currentChunk[i]);
                }

                currentChunk = overlapChunk;
                currentTokenCount = overlapTokens;
            }
        }

        if (currentChunk.Count > 0)
        {
            chunks.Add(string.Join(' ', currentChunk));
        }

        return chunks;
    }

    private static List<string> SplitIntoSentences(string text)
    {
        var sentences = System.Text.RegularExpressions.Regex
            .Split(text, @"(?<=[.!?])\s+")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        return sentences;
    }
}