namespace Simulator.Core;

public interface IMap<TKey, TValue>
{
	public void Insert(TKey key, TValue value);

	public bool Get(TKey key, ref TValue value);

	public void Clear(IExecutor executor);
}
