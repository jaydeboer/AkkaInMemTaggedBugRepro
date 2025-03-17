using Akka.Actor;
using Akka.Configuration;
using Akka.Hosting;
using Akka.Hosting.TestKit;
using Akka.Persistence.Hosting;
using Akka.Persistence.Query;
using Akka.Persistence.Query.InMemory;

namespace AkkaInMemTaggedBug;

public class UnitTest1 : TestKit
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ShouldReturnEventEnvelopeWithOriginalPayloadType(bool withWorkaround)
    {
        var writer = Sys.ActorOf(Props.Create<EventWriterActor>(), "writer");
        Sys.ActorOf(Props.Create<ReaderActor>("writer", TestActor, withWorkaround), "reader");
        
        writer.Tell(new TaggedEvent());
        ExpectMsg<TaggedEvent>();

        var ee = ExpectMsg<EventEnvelope>();
        Assert.IsType<TaggedEvent>(ee.Event);
        Assert.Contains("myTag", ee.Tags);
    }
    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.WithInMemoryJournal().WithInMemorySnapshotStore()
            .AddHocon(InMemoryReadJournal.DefaultConfiguration(), HoconAddMode.Append)
            .AddHocon(ConfigurationFactory.ParseString(
                $$"""
                         akka.persistence.query.journal.inmem.refresh-interval = 0.25s
                         akka.persistence.journal.inmem {
                             event-adapters {
                                 workflow-event-tagger = "{{typeof(DummyTagger).FullName}}, {{typeof(DummyTagger).Assembly}}"
                             }
                             event-adapter-bindings {
                                 "{{typeof(ITaggedEvent).FullName}}, {{typeof(ITaggedEvent).Assembly}}" = workflow-event-tagger
                             }
                         }
                     """), HoconAddMode.Prepend);
    }
}

public interface ITaggedEvent;

public record TaggedEvent : ITaggedEvent;

