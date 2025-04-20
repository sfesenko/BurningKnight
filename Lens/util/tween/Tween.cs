using System;
using System.Collections.Generic;
using System.Reflection;

namespace Lens.util.tween;

public static class Tween {
	private static readonly List<TweenTask?> _tasks = [];

	public static TweenTask To(float to, float value, Action<float> set, float duration, Func<float, float>? ease = null, float delay = 0) {
		ease ??= Ease.QuadOut;
		
		var task = new TweenTask();
		_tasks.Add(task);
		
		task.Delay = delay;
		task.Duration = duration;
		task.EaseFn = ease;
		task.Single = true;
		task.From = value;
		task.To = to;
		task.Set = set;
	
		return task;
	}

	public static TweenTask To<T>(T target, object? values, float duration, Func<float, float>? ease = null, float delay = 0) {
		ease ??= Ease.QuadOut;
		
		var task = new TweenTask();
		_tasks.Add(task);
		
		task.Delay = delay;
		task.Duration = duration;
		task.EaseFn = ease;
		
		if (values == null) {
			return task;
		}
		
		foreach (var property in values.GetType().GetTypeInfo().DeclaredProperties) {
			try {
				var info = new TweenValue(target, property.Name);
				var to = Convert.ToSingle(new TweenValue(values, property.Name, false).Value);

				var s = Convert.ToSingle(info.Value);
				var r = to - s;

				task.Vars.Add(info);
				task.Start.Add(s);
				task.Range.Add(r);
			} catch (Exception e) {
				Log.Error(e);
			}
		}
	
		return task;
	}

	public static void Remove(TweenTask? task) {
		if (task != null) {
			_tasks.Remove(task);
		}
	}
	
	public static void Update(float dt) {
		var i = _tasks.Count - 1;
		
		try {
			for (; i >= 0; i--) {
				var task = _tasks[i];

				if (task == null) { // A bug that used to happen somehow
					_tasks.RemoveAt(i);
					continue;
				}

				task.Update(dt);

				if (task.Ended) {
					_tasks.RemoveAt(i);
				}
			}
		} catch (Exception e) {
			_tasks.RemoveAt(i);
			Log.Error(e);
		}
	}
}
