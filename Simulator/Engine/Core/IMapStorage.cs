namespace Simulator.Engine.Core;

public interface IMapStorage<TKey, TValue>
{
	public int Size { get; }

	public TKey GetKey(int index);

	public void SetKey(int index, TKey key);

	public TValue GetValue(int index);

	public void SetValue(int index, TValue value);

	public int GetFlag(int index);

	public void SetFlag(int index, int flag);

	public int CompareExchangeFlag(int index, int comparand, int value);
}
