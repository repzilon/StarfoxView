using System;

namespace StarFox.Interop.BSP
{
	public struct BSPIOWriteResult
	{
		/// <summary>
		/// Generally a <see cref="Type"/> descriptor that can be used to reflect the type -- can be blank.
		/// </summary>
		public string Descriptor { get; }

		/// <summary>
		/// The message for this result
		/// </summary>
		public string Message { get; }

		/// <summary>
		/// A value indicating whether the model was ultimately exported to a file
		/// </summary>
		public bool Successful { get; }

		internal BSPIOWriteResult(string descriptor, string message, bool successful)
		{
			Descriptor = descriptor;
			Message = message;
			Successful = successful;
		}

		public static BSPIOWriteResult Cancelled = new BSPIOWriteResult("Cancelled", "Operation was cancelled.", false);
		public static BSPIOWriteResult Faulted(Exception exception) => new BSPIOWriteResult(exception.GetType().Name, $"An error has occurred: {exception.Message}", false);
	}
}
