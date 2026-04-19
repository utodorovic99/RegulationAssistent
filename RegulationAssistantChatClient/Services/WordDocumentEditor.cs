using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Office.Interop.Word;

namespace RegulationAssistantChatClient.Services
{
	/// <summary>
	/// Manages opening and monitoring Word documents with event handling for save and close operations.
	/// </summary>
	public class WordDocumentEditor : IDisposable
	{
		private Application? wordApp;
		private Document? document;
		private string? tempFilePath;
		private bool isNewDocument;
		private bool disposed;
		private bool documentWasModified;

		/// <summary>
		/// Event raised when the document is closed.
		/// </summary>
		public event EventHandler<DocumentClosedEventArgs>? DocumentClosed;

		/// <summary>
		/// Event raised when the document is saved.
		/// </summary>
		public event EventHandler? DocumentSaved;

		/// <summary>
		/// Opens a document in Microsoft Word with event monitoring.
		/// </summary>
		/// <param name="fileBytes">The document bytes to open.</param>
		/// <param name="fileName">The name of the file (used for temp file naming).</param>
		/// <param name="versionNumber">The version number of the document.</param>
		/// <returns>True if the document was opened successfully; otherwise false.</returns>
		public async System.Threading.Tasks.Task<bool> OpenDocumentAsync(byte[] fileBytes, string fileName, int versionNumber)
		{
			try
			{
				// Create a temporary file with the document bytes
				string tempPath = Path.GetTempPath();
				string fileExtension = Path.GetExtension(fileName);
				if (string.IsNullOrWhiteSpace(fileExtension))
				{
					fileExtension = ".docx";
				}

				string tempFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_v{versionNumber}_{Guid.NewGuid()}{fileExtension}";
				tempFilePath = Path.Combine(tempPath, tempFileName);

				await System.Threading.Tasks.Task.Run(() => File.WriteAllBytes(tempFilePath, fileBytes));

				// Start Word application
				wordApp = new Application();
				wordApp.Visible = true;

				// Open the document
				document = wordApp.Documents.Open(tempFilePath);

				// Mark this as an existing document (not a newly created blank doc)
				isNewDocument = false;

				// Hook up event handlers
				((ApplicationEvents4_Event)wordApp).DocumentBeforeSave += WordApp_DocumentBeforeSave;
				((ApplicationEvents4_Event)wordApp).DocumentBeforeClose += WordApp_DocumentBeforeClose;

				return true;
			}
			catch (Exception)
			{
				CleanupResources();
				return false;
			}
		}

		/// <summary>
		/// Creates a new blank document in Microsoft Word with event monitoring.
		/// </summary>
		/// <param name="defaultFileName">The default file name (without extension) for the document.</param>
		/// <returns>True if the document was created successfully; otherwise false.</returns>
		public System.Threading.Tasks.Task<bool> CreateNewDocumentAsync(string defaultFileName = "NewDocument")
		{
			try
			{
				// Create temp file path for the new document
				string tempPath = Path.GetTempPath();
				string tempFileName = $"{defaultFileName}_{Guid.NewGuid()}.docx";
				tempFilePath = Path.Combine(tempPath, tempFileName);

				// Start Word application
				wordApp = new Application();
				wordApp.Visible = true;

				// Create a new blank document
				document = wordApp.Documents.Add();

				// Immediately save the new blank document to the temp path so Word has a file backing it
				try
				{
					document.SaveAs2(tempFilePath);
				}
				catch
				{
					// ignore save failures for now; we'll attempt to save again on close
				}

				// Mark this as a new document
				isNewDocument = true;

				// Hook up event handlers
				((ApplicationEvents4_Event)wordApp).DocumentBeforeSave += WordApp_DocumentBeforeSave;
				((ApplicationEvents4_Event)wordApp).DocumentBeforeClose += WordApp_DocumentBeforeClose;

				return System.Threading.Tasks.Task.FromResult(true);
			}
			catch (Exception)
			{
				CleanupResources();
				return System.Threading.Tasks.Task.FromResult(false);
			}
		}

		/// <summary>
		/// Event handler for when a document is about to be saved.
		/// </summary>
		private void WordApp_DocumentBeforeSave(Document doc, ref bool saveAsUI, ref bool cancel)
		{
			// Determine if this save applies to our tracked document.
			bool applies = false;

			// First try comparing COM identities
			if (IsSameComObject(doc, document))
			{
				applies = true;
			}
			else
			{
				// Fall back to path comparison when available (handles Save As creating a new COM object)
				try
				{
					string? docPath = GetDocumentPathSafe(doc);
					if (!string.IsNullOrEmpty(docPath) && !string.IsNullOrEmpty(tempFilePath))
					{
						if (string.Equals(Path.GetFullPath(docPath), Path.GetFullPath(tempFilePath), StringComparison.OrdinalIgnoreCase))
						{
							applies = true;
						}
					}
				}
				catch
				{
					// ignore
				}
			}

			if (applies)
			{
				// Mark that the document has been saved/modified.
				documentWasModified = true;
				DocumentSaved?.Invoke(this, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Attempts to get a safe full path for a COM Document; catches COM exceptions.
		/// </summary>
		private static string? GetDocumentPathSafe(Document doc)
		{
			try
			{
				string fullName = doc.FullName; // may throw if unsaved
				return string.IsNullOrEmpty(fullName) ? null : fullName;
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Event handler for when a document is about to be closed.
		/// Uses the boolean marker set by the save event to detect modifications instead of hash comparisons.
		/// </summary>
		private void WordApp_DocumentBeforeClose(Document doc, ref bool cancel)
		{
			// Only proceed if this close applies to our tracked document
			bool applies = false;
			if (IsSameComObject(doc, document)) applies = true;
			else
			{
				try
				{
					string? docPath = GetDocumentPathSafe(doc);
					if (!string.IsNullOrEmpty(docPath) && !string.IsNullOrEmpty(tempFilePath))
					{
						applies = string.Equals(Path.GetFullPath(docPath), Path.GetFullPath(tempFilePath), StringComparison.OrdinalIgnoreCase);
					}
				}
				catch { }
			}

			if (!applies) return;

			// Capture modification state and target path/title now. Do NOT read file bytes here because Word still holds file lock.
			bool wasModified = documentWasModified;
			string? documentTitle = null;
			string targetPath = tempFilePath ?? string.Empty;

			try
			{
				if (isNewDocument && document != null)
				{
					// New document: check content to see if user added anything
					string? content = null;
					try
					{
						content = document.Content?.Text;
					}
					catch { }

					bool hasContent = !string.IsNullOrWhiteSpace(content) && content.Trim().Length > 0 && content.Trim() != "\r";

					if (!hasContent)
					{
						// Document left empty - do not save/upload. Ensure temp file is removed during cleanup.
						wasModified = false;
					}
					else
					{
						// Save the document to our temp path (if not already saved there)
						string? docPath = GetDocumentPathSafe(document);
						if (string.IsNullOrEmpty(docPath) || !string.Equals(Path.GetFullPath(docPath), Path.GetFullPath(tempFilePath ?? string.Empty), StringComparison.OrdinalIgnoreCase))
						{
							try { document.SaveAs2(tempFilePath); }
							catch { }
						}

						wasModified = true;
						documentTitle = Path.GetFileName(tempFilePath);
					}
				}
				else if (tempFilePath != null)
				{
					// Existing document: make sure saved
					if (document != null)
					{
						document.Save();
						System.Threading.Thread.Sleep(300);
					}

					// Determine actual path (handles Save As)
					try
					{
						string? docPath = GetDocumentPathSafe(document);
						if (!string.IsNullOrEmpty(docPath)) targetPath = docPath;
					}
					catch { }

					if (wasModified)
					{
						documentTitle = Path.GetFileName(targetPath);
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error preparing DocumentBeforeClose: {ex.Message}");
			}

			// Schedule background work to wait until Word releases the file, then read bytes and raise event.
			_ = System.Threading.Tasks.Task.Run(async () =>
			{
				byte[]? currentFileBytes = null;
				if (wasModified && !string.IsNullOrEmpty(targetPath))
				{
					// Wait a short period first to allow Word to finish closing
					await System.Threading.Tasks.Task.Delay(500);

					// Try repeatedly until file can be read or timeout
					int attempts = 0;
					const int maxAttempts = 30; // ~9 seconds with 300ms delay
					while (attempts < maxAttempts)
					{
						try
						{
							if (File.Exists(targetPath))
							{
								currentFileBytes = File.ReadAllBytes(targetPath);
								break;
							}
							break; // file not found, stop trying
						}
						catch (IOException)
						{
							await System.Threading.Tasks.Task.Delay(300);
							attempts++;
						}
						catch
						{
							break;
						}
					}
				}

				// Raise the closed event (on background thread). Subscriber may marshal to UI if needed.
				var eventArgs = new DocumentClosedEventArgs(wasModified, currentFileBytes, targetPath, documentTitle);
				DocumentClosed?.Invoke(this, eventArgs);

				// Now cleanup resources
				try
				{
					// If the document was new and left empty, delete the temp file now
					if (isNewDocument && !wasModified && !string.IsNullOrEmpty(targetPath) && File.Exists(targetPath))
					{
						try { File.Delete(targetPath); } catch { }
					}

					CleanupResources();
				}
				catch { }
			});
		}

		/// <summary>
		/// Compares two COM objects by comparing their underlying IUnknown pointers.
		/// </summary>
		private static bool IsSameComObject(object? a, object? b)
		{
			if (a == null || b == null) return false;

			IntPtr ptrA = IntPtr.Zero;
			IntPtr ptrB = IntPtr.Zero;

			try
			{
				ptrA = Marshal.GetIUnknownForObject(a);
				ptrB = Marshal.GetIUnknownForObject(b!);

				return ptrA == ptrB;
			}
			catch
			{
				return false;
			}
			finally
			{
				if (ptrA != IntPtr.Zero) Marshal.Release(ptrA);
				if (ptrB != IntPtr.Zero) Marshal.Release(ptrB);
			}
		}

		/// <summary>
		/// Cleans up Word application resources and temporary files.
		/// </summary>
		private void CleanupResources()
		{
			try
			{
				// Close document if still open
				if (document != null)
				{
					document.Close(SaveChanges: false);
					Marshal.ReleaseComObject(document);
					document = null;
				}

				// Quit Word application
				if (wordApp != null)
				{
					wordApp.Quit(SaveChanges: false);
					Marshal.ReleaseComObject(wordApp);
					wordApp = null;
				}

				// Delete temp file
				if (tempFilePath != null && File.Exists(tempFilePath))
				{
					try
					{
						File.Delete(tempFilePath);
					}
					catch (IOException)
					{
						// File might still be locked, try again after delay
						System.Threading.Tasks.Task.Run(async () =>
						{
							await System.Threading.Tasks.Task.Delay(2000);
							try
							{
								if (File.Exists(tempFilePath))
								{
									File.Delete(tempFilePath);
								}
							}
							catch
							{
								// Ignore if still can't delete
							}
						});
					}
				}
			}
			catch (Exception)
			{
				// Ignore cleanup errors
			}
		}

		/// <summary>
		/// Disposes the Word document editor and cleans up resources.
		/// </summary>
		public void Dispose()
		{
			if (!disposed)
			{
				CleanupResources();
				disposed = true;
			}
			GC.SuppressFinalize(this);
		}
	}

	/// <summary>
	/// Event arguments for when a Word document is closed.
	/// </summary>
	public class DocumentClosedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DocumentClosedEventArgs"/> class.
		/// </summary>
		/// <param name="wasModified">Indicates whether the document was modified.</param>
		/// <param name="fileBytes">The current file bytes if modified; otherwise null.</param>
		/// <param name="filePath">The path to the temporary file.</param>
		/// <param name="documentTitle">The title/name of the document file.</param>
		public DocumentClosedEventArgs(bool wasModified, byte[]? fileBytes, string? filePath, string? documentTitle = null)
		{
			WasModified = wasModified;
			FileBytes = fileBytes;
			FilePath = filePath;
			DocumentTitle = documentTitle;
		}

		/// <summary>
		/// Gets a value indicating whether the document was modified.
		/// </summary>
		public bool WasModified { get; }

		/// <summary>
		/// Gets the modified file bytes, or null if not modified.
		/// </summary>
		public byte[]? FileBytes { get; }

		/// <summary>
		/// Gets the temporary file path.
		/// </summary>
		public string? FilePath { get; }

		/// <summary>
		/// Gets the document title/name.
		/// </summary>
		public string? DocumentTitle { get; }
	}
}