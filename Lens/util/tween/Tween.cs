using System;
using System.Collections.Generic;
using System.Reflection;

namespace Lens.util.tween;

public  class Tween
{
	public static readonly Tween Instance = new();
	
	public static TweenTask To(float to, float value, Action<float> set, float duration, Func<float, float>? ease = null, float delay = 0) {
		ease ??= Ease.QuadOut;
		
		var task = new TweenTask
		{
			Delay = delay,
			Duration = duration,
			EaseFn = ease,
			Single = true,
			From = value,
			To = to,
			Set = set
		};

		Instance.Add(task);

		return task;
	}

	public static TweenTask To<T>(T target, object? values, float duration, Func<float, float>? ease = null, float delay = 0) {
		ease ??= Ease.QuadOut;
		
		var task = new TweenTask
		{
			Delay = delay,
			Duration = duration,
			EaseFn = ease
		};

		if (values == null) {
			return task;
		}
		
		foreach (var property in values.GetType().GetTypeInfo().DeclaredProperties) {
			try {
				var info = new TweenValue(target, property.Name);
				var to = Convert.ToSingle(new TweenValue(values, property.Name, false).Value);

				float s = Convert.ToSingle(info.Value);
				float r = to - s;

				task.Vars.Add(info);
				task.Start.Add(s);
				task.Range.Add(r);
			} catch (Exception e) {
				Log.Error(e);
			}
		}

		Instance.Add(task);

		return task;
	}

	/**
	 * 
	 */
	private  readonly HashSet<TweenTask> _tasks = [];
	private  readonly System.Collections.Concurrent.ConcurrentBag<TweenTask> _newTasks = [];

	private Tween() {}
	
	private void Add(TweenTask task)
	{
		_newTasks.Add(task);
	}

	private int _maxSize = 0;
	
	public void Update(float dt)
	{
		var size = _tasks.Count;
		var count = 0;
		while (_tasks.Count < size + _newTasks.Count)
		{
			_tasks.UnionWith(_newTasks);
			count += 1;
		}
		_newTasks.Clear();
		if (count > 1)
		{
			Log.Warning("Update: multiple merge");
		}

		_maxSize = Math.Max(_maxSize, _tasks.Count);
		
		var toRemove = new List<TweenTask>();
		foreach (var task in _tasks)
		{
			task.Update(dt);
			if (task.Ended)
			{
				toRemove.Add(task);
			}
		}
		
		foreach (var t in toRemove)
		{
			_tasks.Remove(t);
		}
	}
}
