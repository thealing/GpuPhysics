namespace Simulator.Backend.Cpu;

using System;
using Simulator.Backend.Common;
using Simulator.Core;
using Simulator.Engine.Physics.Simulation;

public struct ContactStorage
{
	public Box<int> ContactCount;
	public Contact[] Contacts;
	public ContactBodyLink[] ContactBodyLinks;
	public ContactShapeLink[] ContactShapeLinks;
	public SplitContact[] SplitContacts;
	public Map<ContactShapeLink, ContactCache, MapStorage<ContactShapeLink, ContactCache>> CacheMap;

	public ContactStorage()
	{
		ContactCount = new Box<int>();
		Contacts = Array.Empty<Contact>();
		ContactBodyLinks = Array.Empty<ContactBodyLink>();
		ContactShapeLinks = Array.Empty<ContactShapeLink>();
		SplitContacts = Array.Empty<SplitContact>();
		CacheMap.Storage = new MapStorage<ContactShapeLink, ContactCache>();
	}

	public void SetCapacity(int capacity)
	{
		if (capacity <= Contacts.Length)
		{
			return;
		}
		Array.Resize(ref Contacts, capacity);
		Array.Resize(ref ContactBodyLinks, capacity);
		Array.Resize(ref ContactShapeLinks, capacity);
		Array.Resize(ref SplitContacts, capacity * 2);
		CacheMap.Storage.SetCapacity(capacity * 2);
	}

	public void Reset()
	{
		ContactCount.Value = 0;
	}

	public int Add(int shapeIndexA, int shapeIndexB, int bodyIndexA, int bodyIndexB, Contact contact)
	{
		Atomic atomic = new Atomic();
		int contactIndex = Limited.Increment(atomic, ref ContactCount.Value, Contacts.Length);
		if (contactIndex != -1)
		{
			Contacts[contactIndex] = contact;
			ContactShapeLinks[contactIndex] = new ContactShapeLink(shapeIndexA, shapeIndexB);
			ContactBodyLinks[contactIndex] = new ContactBodyLink(bodyIndexA, bodyIndexB);
		}
		return contactIndex;
	}

	public void ClearContactCache(IExecutor executor)
	{
		CacheMap.Clear(executor);
	}

	public void SaveContactCache(int index)
	{
		ContactCache cache = new ContactCache(Contacts[index]);
		CacheMap.Insert(ContactShapeLinks[index], cache);
	}

	public bool LoadContactCache(int index, ref ContactCache cache)
	{
		return CacheMap.Get(ContactShapeLinks[index], ref cache);
	}
}
