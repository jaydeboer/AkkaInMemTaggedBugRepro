using Akka.Actor;
using Akka.Persistence.Journal;
using Akka.Persistence.Query;
using Akka.Persistence.Query.InMemory;
using Akka.Streams;
using Akka.Streams.Dsl;

namespace AkkaInMemTaggedBug;

public class ReaderActor : ReceiveActor
{
    private readonly IActorRef _destActorRef;

    public ReaderActor(string persistenceId, IActorRef destActorRef, bool withWorkAround = false)
    {
        _destActorRef = destActorRef;
        var readJournal = Context.System.ReadJournalFor<InMemoryReadJournal>(InMemoryReadJournal.Identifier);
        var materializer = ActorMaterializer.Create(Context.System);
        var source = readJournal.EventsByPersistenceId(persistenceId, 0L , long.MaxValue);
        
        // I originally tried this with `.RunForeach` but it was not working as expected, that seems to not send
        // the events to the actor, but rather just run the code for each event, without waiting for the previous
        // one to finish. So I switched to `RunWith` and used an actor sink. That way the events never overlap.
        source
            .Select(ee =>  // This is to fix an issue with in memory journal where the event is tagged.  Other (MongoDB) journals do not have this issue.
            {
                if (withWorkAround && ee.Event is Tagged tagged)
                    return new EventEnvelope(ee.Offset, ee.PersistenceId, ee.SequenceNr, tagged.Payload, ee.Timestamp, [..tagged.Tags]);
                return ee;
            })
            .Where(ee => ee.Event is ITaggedEvent)
            .RunWith(Sink.ActorRef<EventEnvelope>(Self,
                StreamFailed.Instance, ex => ex), materializer);
        
        Receive<EventEnvelope>(HandleEnvelope);
    }

    private void HandleEnvelope(EventEnvelope ee)
    {
        _destActorRef.Tell(ee);
    }
}

public class StreamFailed
{
    public static readonly StreamFailed Instance = new();
}