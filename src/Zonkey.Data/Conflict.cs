namespace Zonkey
{
    /// <summary>
    /// Provides a class for describing conflicts when executing commands on a database
    /// </summary>
    public class Conflict
    {
        /// <summary>
        /// Default constructor - initializes a new instance of the <see cref="Conflict"/> class.
        /// </summary>
        public Conflict()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Conflict"/> class.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="originalValue">The original value.</param>
        /// <param name="currentDbValue">The current DB value.</param>
        /// <param name="attemptedValue">The attempted value.</param>
        public Conflict(string propertyName, object originalValue, object currentDbValue, object attemptedValue)
        {
            PropertyName = propertyName;
            OriginalValue = originalValue;
            CurrentDbValue = currentDbValue;
            AttemptedValue = attemptedValue;
        }

        /// <summary>
        /// Gets or sets the name of the property.
        /// </summary>
        /// <value>The name of the property.</value>
        public string PropertyName { get; set; }

        /// <summary>
        /// Gets or sets the original value.
        /// </summary>
        /// <value>The original value.</value>
        public object OriginalValue { get; set; }

        /// <summary>
        /// Gets or sets the current DB value.
        /// </summary>
        /// <value>The current DB value.</value>
        public object CurrentDbValue { get; set; }

        /// <summary>
        /// Gets or sets the attempted value.
        /// </summary>
        /// <value>The attempted value.</value>
        public object AttemptedValue { get; set; }

        /// <summary>
        /// Gets or sets the set value to.
        /// </summary>
        /// <value>The set value to.</value>
        public object SetValueTo { get; set; }
    }
}