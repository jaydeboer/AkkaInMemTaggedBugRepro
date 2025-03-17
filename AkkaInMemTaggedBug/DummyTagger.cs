using System.Collections.Immutable;
using Akka.Persistence.Journal;

namespace AkkaInMemTaggedBug;

public class DummyTagger: IWriteEventAdapter
{
    public const string Tag = "myTag";
    public string Manifest(object evt)
    {
        return string.Empty;
    }

    public object ToJournal(object evt)
    {
        var tags = new HashSet<string>();
        switch (evt)
        {
            case ITaggedEvent :
                tags.Add(Tag);
                break;
        }
        return tags.Count > 0 ? new Tagged(evt, tags.ToImmutableHashSet()) : evt;
    }
}