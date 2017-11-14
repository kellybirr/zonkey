using System;
using System.ComponentModel;

namespace Zonkey.ObjectModel
{
    /// <summary>
    /// 
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
    public abstract class DataComponent : DataClass, IComponent, IDisposable
    {
        /// <summary>
        /// Occurs when the <see cref="Zonkey.ObjectModel.DataClass"/>'s Dispose() method is called.
        /// </summary>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public event EventHandler Disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Zonkey.ObjectModel.DataComponent"/> class.
        /// </summary>
        /// <param name="addingNew">if set to <c>true</c> then initializes object for insertion to database.</param>
        protected DataComponent(bool addingNew) : base(addingNew)
        {
        }

         ~DataComponent()
        {
            Dispose(false);
        }

        #region IComponent Members

        /// <summary>
        /// Gets or sets the <see cref="T:System.ComponentModel.ISite"></see> associated with the <see cref="T:System.ComponentModel.IComponent"></see>.
        /// </summary>
        /// <value></value>
        /// <returns>The <see cref="T:System.ComponentModel.ISite"></see> object associated with the component; or null, if the component does not have a site.</returns>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        ISite IComponent.Site { get; set; }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                Disposed?.Invoke(this, EventArgs.Empty);
        }

        #endregion


    }
}