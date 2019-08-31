public class CircularArray<T>
{
	public T [] Array { get; private set; }
	public int Length { get; private set; }
	public int CurrentIndex { get; private set; }

	public CircularArray (int length)
	{
		Array = new T [length];
		Length = length;
	}

	public T this [int index]
	{
		get { return Array [GetIndex (index)]; }
		set { Array [GetIndex (index)] = value; }
	}

	public int GetIndex (int index)
	{
		if (Length == 0)
			throw new System.ArgumentOutOfRangeException ("There are no spaces for any items in this circular array.");

		if (index == 0)
			return CurrentIndex;

		index += CurrentIndex;
		index = index % Length;

		if (index < 0)
			index += Length;

		return index;
	}

	public void Push (T item)
	{
		CurrentIndex += 1;
		if (CurrentIndex >= Length)
			CurrentIndex = 0;

		this [0] = item;
	}
}
