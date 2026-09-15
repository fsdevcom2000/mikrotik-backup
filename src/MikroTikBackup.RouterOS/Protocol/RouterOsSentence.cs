namespace MikroTikBackup.RouterOS.Protocol;

public static class RouterOsSentence
{
    public static RouterOsResponse Parse(
        IReadOnlyList<string> words)
    {
        if (words.Count == 0)
        {
            throw new InvalidDataException(
                "RouterOS returned an empty sentence.");
        }

        var response = new RouterOsResponse
        {
            Type = words[0]
        };

        for (var i = 1; i < words.Count; i++)
        {
            var word = words[i];

            response.Words.Add(word);

            if (!word.StartsWith("="))
                continue;

            var separator = word.IndexOf(
                '=',
                1);

            if (separator <= 1)
                continue;

            var name = word[1..separator];
            var value = word[(separator + 1)..];

            response.Attributes[name] = value;
        }

        return response;
    }
}