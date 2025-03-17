using Akka.Actor;
using Akka.Persistence;

namespace AkkaInMemTaggedBug;

public class EventWriterActor : ReceivePersistentActor
{
    public override string PersistenceId => Self.Path.Name;

    public EventWriterActor()
    {
        CommandAny(OnReceiveCommand);
        RecoverAny(_ => { });
    }
    private void OnReceiveCommand(object obj)
    {
        Persist(obj, Sender.Tell);
    }
}