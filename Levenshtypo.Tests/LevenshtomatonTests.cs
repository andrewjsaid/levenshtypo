using Levenshtypo;
using Shouldly;

namespace Tests;

public class LevenshtomatonTests
{
    [Theory]
    [InlineData("abcd")]
    [InlineData("bbbbbbbb")]
    [InlineData("food")]
    [InlineData("goodmood")]
    [InlineData("ahab")]
    [InlineData("abcdefgh")]
    public void Tests(string word)
    {
        var factory = new LevenshtomatonFactory();
        var automaton0 = factory.Construct(word, 0);
        var automaton1 = factory.Construct(word, 1);
        var automaton2 = factory.Construct(word, 2);

        foreach (var (testWord, distance) in WithAtMostNChanges(word, maxIterations: 100_000))
        {
            automaton0.Matches(testWord).ShouldBe(distance <= 0);
            automaton1.Matches(testWord).ShouldBe(distance <= 1);
            automaton2.Matches(testWord).ShouldBe(distance <= 2);
        }
    }

    private IEnumerable<(string newWord, int changes)> WithAtMostNChanges(string query, int maxIterations)
    {
        if (query.Contains("~"))
        {
            throw new InvalidOperationException();
        }

        var seen = new HashSet<string>();
        seen.Add(query);
        yield return (query, 0);
        maxIterations--;

        var queue = new Queue<string>();
        queue.Enqueue(query);

        while (queue.Count > 0 && maxIterations > 0)
        {
            var word = queue.Dequeue();
            foreach (var changedWord in With1Change(word))
            {
                if (seen.Add(changedWord))
                {
                    var distance = LevenshteinDistance.CalculateCaseSensitive(query, changedWord);
                    yield return (changedWord, distance);
                    maxIterations--;
                    queue.Enqueue(changedWord);
                }
            }
        }

        IEnumerable<string> With1Change(string word)
        {
            var chars = word.ToCharArray();
            var builder = new List<char>();

            var distinctChars = new HashSet<char>();

            for (int i = 0; i < word.Length; i++)
            {
                ResetBuilder();
                builder.RemoveAt(i);
                yield return CreateString();
            }

            for (int i = 0; i < word.Length; i++)
            {
                // Substitute random character
                ResetBuilder();
                builder[i] = '~';
                yield return CreateString();

                distinctChars.Clear();
                // Substitute character which appears later
                for (int copyIndex = i; copyIndex < word.Length; copyIndex++)
                {
                    var newChar = word[copyIndex];
                    if (builder[i] != newChar)
                    {
                        ResetBuilder();
                        builder[i] = newChar;
                        yield return CreateString();
                    }
                }
            }

            for (int i = 0; i < word.Length + 1; i++)
            {
                // Insert random character
                ResetBuilder();
                builder.Insert(i, '~');
                yield return CreateString();

                distinctChars.Clear();
                // Insert character which appears later
                for (int copyIndex = i; copyIndex < word.Length; copyIndex++)
                {
                    var newChar = word[copyIndex];
                    if (builder[i] != newChar)
                    {
                        ResetBuilder();
                        builder.Insert(i, newChar);
                        yield return CreateString();
                    }
                }
            }

            void ResetBuilder()
            {
                builder.Clear();
                builder.AddRange(chars);
            }

            string CreateString()
            {
                return string.Create(builder.Count, builder, static (buffer, builder) =>
                {
                    builder.CopyTo(buffer);
                });
            }
        }
    }
}
