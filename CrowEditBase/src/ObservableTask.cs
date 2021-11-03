// Copyright (c) 2021-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CrowEditBase
{
	public class ObservableTask<T> : Task<T> {
		//;
		public ObservableTask (Func<T> func, CancellationToken cancellationToken = new CancellationToken(), TaskCreationOptions options = TaskCreationOptions.None) :
			base (func, cancellationToken, options) {
				
		}

		 
	}
}

