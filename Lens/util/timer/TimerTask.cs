using System;

namespace Lens.util.timer;

public class TimerTask(Action fn, float delay)
{
	public float Delay = delay;
	public readonly Action Fn = fn;

	public void Cancel() {
		Timer.Cancel(this);
	}
}
