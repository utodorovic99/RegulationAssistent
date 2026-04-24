using System.Text;
using AuditService.Model;

namespace AuditService.AuditTable
{
	internal sealed class QueryTraceFormatter : IQueryTraceFormatter
	{
		public string Format(QueryAudit queryAudit)
		{
			if (queryAudit == null)
			{
				return string.Empty;
			}

			StringBuilder sb = new StringBuilder();

			sb.AppendLine($"Query Id: {queryAudit.QueryId}");
			sb.AppendLine($"Received Time: {queryAudit.ReceivedTime}");
			sb.AppendLine($"Query Text: {queryAudit.QueryText}");

			if (queryAudit?.AdditionalContext != null)
			{
				sb.AppendLine($"Query Additional Context:");

				if (queryAudit.AdditionalContext.Date != default)
				{
					sb.AppendLine($"\t- Filter Time: {queryAudit.AdditionalContext.Date}");
				}
			}

			sb.AppendLine();

			if (queryAudit.ResponseAudit != null)
			{
				sb.AppendLine("Response:");
				sb.AppendLine($"\t- Answer: {queryAudit.ResponseAudit.Answer}");
				sb.AppendLine($"\t- Generated Time: {queryAudit.ResponseAudit.GeneratedTime}");
				sb.AppendLine($"\t- Status: {queryAudit.ResponseAudit.Status}");
				sb.AppendLine($"\t- Confidence: {queryAudit.ResponseAudit.Confidence:F2}");
			}

			sb.AppendLine();

			if (queryAudit?.ServiceTracing?.Count > 0)
			{
				int i = 1;
				foreach (var serviceTrace in queryAudit.ServiceTracing)
				{
					sb.AppendLine($"{i++}. {serviceTrace.ServiceName}");
					sb.AppendLine($"Enter Time: {serviceTrace.EnterTime}");
					sb.AppendLine($"Exit Time: {serviceTrace.ExitTime}");
				}
			}

			sb.AppendLine();

			if (queryAudit.Events?.Count > 0)
			{
				sb.AppendLine($"Events:");

				foreach (var serviceEvent in queryAudit.Events)
				{
					sb.AppendLine($"\t- [{serviceEvent.Timestamp}][{serviceEvent.Service}]({serviceEvent.Status}) {serviceEvent.Message}");
				}
			}

			return sb.ToString();
		}
	}
}
