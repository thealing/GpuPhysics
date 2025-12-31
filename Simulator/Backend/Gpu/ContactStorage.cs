namespace Simulator.Backend.Gpu;

using ILGPU;
using ILGPU.Runtime;
using Simulator.Backend.Common;
using Simulator.Engine.Core;
using Simulator.Engine.Physics.Simulation;

public struct ContactStorage
{
	public ArrayView<Contact> Contacts;
	public ArrayView<ContactBodyLink> ContactBodyLinks;
	public ArrayView<ContactShapeLink> ContactShapeLinks;
	public ArrayView<SplitContact> SplitContacts;
	public ArrayView<int> ContactCount;
	public Map<ContactShapeLink, ContactCache, MapStorage<ContactShapeLink, ContactCache>> CacheMap;

	public ContactStorage(Accelerator accelerator)
	{
		Contacts = accelerator.AllocateZeroedView<Contact>(0);
		ContactBodyLinks = accelerator.AllocateZeroedView<ContactBodyLink>(0);
		ContactShapeLinks = accelerator.AllocateZeroedView<ContactShapeLink>(0);
		SplitContacts = accelerator.AllocateZeroedView<SplitContact>(0);
		ContactCount = accelerator.AllocateZeroedView<int>(1);
		CacheMap.Storage = new MapStorage<ContactShapeLink, ContactCache>(accelerator);
	}

	public int GetContactCountOnCpu()
	{
		int count = 0;
		ContactCount.CopyToCPU(ref count, 1);
		return count;
	}

	public void CopyFromCPU(Cpu.ContactStorage contactStorage)
	{
		int count = contactStorage.ContactCount.Value;
		ContactCount.CopyFromCPU(ref count, 1);
		Contacts.SafeCopyFromCPU(contactStorage.Contacts, count);
		ContactBodyLinks.SafeCopyFromCPU(contactStorage.ContactBodyLinks, count);
		ContactShapeLinks.SafeCopyFromCPU(contactStorage.ContactShapeLinks, count);
		SplitContacts.SafeCopyFromCPU(contactStorage.SplitContacts, count);
		if (CacheMap.Storage.Size != contactStorage.CacheMap.Storage.Size)
		{
			CacheMap.Storage.CopyFromCPU(contactStorage.CacheMap.Storage);
		}
	}

	public void CopyToCPU(Cpu.ContactStorage contactStorage)
	{
		int count = GetContactCountOnCpu();
		contactStorage.ContactCount.Value = count;
		Contacts.CopyToCPU(contactStorage.Contacts, count);
		ContactBodyLinks.CopyToCPU(contactStorage.ContactBodyLinks, count);
		ContactShapeLinks.CopyToCPU(contactStorage.ContactShapeLinks, count);
		SplitContacts.CopyToCPU(contactStorage.SplitContacts, count);
		// Warm-start cache is large, so no copying it.
		//CacheMap.Storage.CopyToCPU(contactStorage.CacheMap.Storage);
	}

	public void Reset()
	{
		ContactCount.MemSetToZero();
	}

	public int Add(int shapeIndexA, int shapeIndexB, int bodyIndexA, int bodyIndexB, Contact contact)
	{
		Atomic atomic = new Atomic();
		int contactIndex = Limited.Increment(atomic, ref ContactCount[0], Contacts.IntLength);
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
