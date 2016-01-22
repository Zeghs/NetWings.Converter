using System;
using PowerLanguage;

namespace Zeghs.Data {
	internal sealed class Tick : ITick {
		public DOMPrice Ask {
			get;
			internal set;
		}

		public DOMPrice Bid {
			get;
			internal set;
		}

		public double Price {
			get;
			internal set;
		}

		public double Single {
			get;
			internal set;
		}

		public DateTime Time {
			get;
			internal set;
		}
		
		public double Volume {
			get;
			internal set;
		}
	}
}