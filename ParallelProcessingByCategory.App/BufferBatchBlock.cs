using System.Threading.Tasks.Dataflow;
using System.Timers;

namespace ParallelProcessingByCategory.App;
public class BufferBatchBlock<T> : IPropagatorBlock<T, T[]>,
    IReceivableSourceBlock<T[]>
{
    // The target part of the block.
    private readonly ITargetBlock<T> m_target;

    // The source part of the block.
    private readonly IReceivableSourceBlock<T[]> m_source;

    // Constructs a SlidingWindowBlock object.
    public BufferBatchBlock(int size, int interval = 1000)
    {
        // Create a queue to hold messages.
        var queue = new Queue<T>();

        // The source part of the propagator holds arrays of size windowSize
        // and propagates data out to any connected targets.
        var source = new BufferBlock<T[]>();

        System.Timers.Timer timer = new System.Timers.Timer(interval);
        timer.Elapsed += (object sender, ElapsedEventArgs e) =>
        {
            Console.WriteLine($"Timer elapsed queue: {queue.Count}");
            source.Post(queue.ToArray());
            queue.Clear();
        };
        timer.AutoReset = false;

        // The target part receives data and adds them to the queue.
        var target = new ActionBlock<T>(item =>
        {
            // Add the item to the queue.
            queue.Enqueue(item);
            timer.Stop();
            timer.Start();
            // Post the data in the queue to the source block when the queue size
            // equals the window size.
            if (queue.Count == size)
            {
                source.Post(queue.ToArray());
                queue.Clear();
                timer.Stop();
            }
        });

        // When the target is set to the completed state, propagate out any
        // remaining data and set the source to the completed state.
        target.Completion.ContinueWith(delegate
        {
            if (queue.Count > 0 && queue.Count < size)
                source.Post(queue.ToArray());
            source.Complete();
        });

        m_target = target;
        m_source = source;
    }

    #region IReceivableSourceBlock<TOutput> members

    // Attempts to synchronously receive an item from the source.
    public bool TryReceive(Predicate<T[]> filter, out T[] item)
    {
        return m_source.TryReceive(filter, out item);
    }

    // Attempts to remove all available elements from the source into a new
    // array that is returned.
    public bool TryReceiveAll(out IList<T[]> items)
    {
        return m_source.TryReceiveAll(out items);
    }

    #endregion

    #region ISourceBlock<TOutput> members

    // Links this dataflow block to the provided target.
    public IDisposable LinkTo(ITargetBlock<T[]> target, DataflowLinkOptions linkOptions)
    {
        return m_source.LinkTo(target, linkOptions);
    }

    // Called by a target to reserve a message previously offered by a source
    // but not yet consumed by this target.
    bool ISourceBlock<T[]>.ReserveMessage(DataflowMessageHeader messageHeader,
        ITargetBlock<T[]> target)
    {
        return m_source.ReserveMessage(messageHeader, target);
    }

    // Called by a target to consume a previously offered message from a source.
    T[] ISourceBlock<T[]>.ConsumeMessage(DataflowMessageHeader messageHeader,
        ITargetBlock<T[]> target, out bool messageConsumed)
    {
        return m_source.ConsumeMessage(messageHeader,
            target, out messageConsumed);
    }

    // Called by a target to release a previously reserved message from a source.
    void ISourceBlock<T[]>.ReleaseReservation(DataflowMessageHeader messageHeader,
        ITargetBlock<T[]> target)
    {
        m_source.ReleaseReservation(messageHeader, target);
    }

    #endregion

    #region ITargetBlock<TInput> members

    // Asynchronously passes a message to the target block, giving the target the
    // opportunity to consume the message.
    DataflowMessageStatus ITargetBlock<T>.OfferMessage(DataflowMessageHeader messageHeader,
        T messageValue, ISourceBlock<T> source, bool consumeToAccept)
    {
        return m_target.OfferMessage(messageHeader,
            messageValue, source, consumeToAccept);
    }

    #endregion

    #region IDataflowBlock members

    // Gets a Task that represents the completion of this dataflow block.
    public Task Completion
    {
        get { return m_source.Completion; }
    }

    // Signals to this target block that it should not accept any more messages,
    // nor consume postponed messages.
    public void Complete()
    {
        m_target.Complete();
    }

    public void Fault(Exception error)
    {
        m_target.Fault(error);
    }

    #endregion
}
