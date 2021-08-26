using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Scripts.Base
{
	public class ObjectPool<T> : IDisposable where T : MonoBehaviour
	{
		private Queue<T> _queue;


		public ObjectPool(T baseItem, int count, Transform parent = null)
		{
			_queue = new Queue<T>();
			for (int i = 0; i < count; i++)
			{
				T t = Object.Instantiate(baseItem, parent);
				t.gameObject.SetActive(false);
				_queue.Enqueue(t);
			}
		}

		public T GetObject()
		{
			T t = _queue.Dequeue();
			t.gameObject.SetActive(true);
			_queue.Enqueue(t);
			return t;
		}

		public void Dispose()
		{
			for (int i = 0; i < _queue.Count; i++)
			{
				Object.Destroy(_queue.Dequeue());
			}

			_queue = null;
		}
	}
}