namespace IRMAKit.Utils
{
	public interface IDrum
	{
		/// <summary>
		/// Add
		/// </summary>
		int Add(string key, object obj);

		/// <summary>
		/// []
		/// </summary>
		object this[string key] { get; }

		/// <summary>
		/// Count
		/// </summary>
		int Count(string key);

		/// <summary>
		/// Is the most recent retrieving operation the end point of a round ?
		/// (it means the next will be a new once begin)
		/// </summary>
		bool RoundEnd(string key);

		/// <summary>
		/// Reset
		/// </summary>
		void Reset(string key);
		
		/// <summary>
		/// Reset all
		/// </summary>
		void ResetAll();

		/// <summary>
		/// Remove
		/// </summary>
		bool Remove(string key);

		/// <summary>
		/// Remove all
		/// </summary>
		void RemoveAll();
	}
}
