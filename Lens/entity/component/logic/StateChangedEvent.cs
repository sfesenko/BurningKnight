using System;

namespace Lens.entity.component.logic {
	public class StateChangedEvent : Event {
		public Type NewState = null!; // set by the sender
		public EntityState State = null!; // set by the sender
	}
}