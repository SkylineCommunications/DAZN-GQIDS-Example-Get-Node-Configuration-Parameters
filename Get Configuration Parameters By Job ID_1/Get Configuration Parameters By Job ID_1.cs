/*
****************************************************************************
*  Copyright (c) 2024,  Skyline Communications NV  All Rights Reserved.    *
****************************************************************************

By using this script, you expressly agree with the usage terms and
conditions set out below.
This script and all related materials are protected by copyrights and
other intellectual property rights that exclusively belong
to Skyline Communications.

A user license granted for this script is strictly for personal use only.
This script may not be used in any way by anyone without the prior
written consent of Skyline Communications. Any sublicensing of this
script is forbidden.

Any modifications to this script by the user are only allowed for
personal use and within the intended purpose of the script,
and will remain the sole responsibility of the user.
Skyline Communications will not be responsible for any damages or
malfunctions whatsoever of the script resulting from a modification
or adaptation by the user.

The content of this script is confidential information.
The user hereby agrees to keep this confidential information strictly
secret and confidential and not to disclose or reveal it, in whole
or in part, directly or indirectly to any person, entity, organization
or administration without the prior written consent of
Skyline Communications.

Any inquiries can be addressed to:

	Skyline Communications NV
	Ambachtenstraat 33
	B-8870 Izegem
	Belgium
	Tel.	: +32 51 31 35 69
	Fax.	: +32 51 31 01 29
	E-mail	: info@skyline.be
	Web		: www.skyline.be
	Contact	: Ben Vandenberghe

****************************************************************************
Revision History:

DATE		VERSION		AUTHOR			COMMENTS

05/12/2024	1.0.0.1		AMA, Skyline	Initial version
****************************************************************************
*/

namespace GetConfigurationParametersByJobID_1
{
	using System;
	using System.Linq;

	using DomHelpers.SlcWorkflow;

	using Newtonsoft.Json.Linq;

	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Represents a data source.
	/// See: https://aka.dataminer.services/gqi-external-data-source for a complete example.
	/// </summary>
	[GQIMetaData(Name = "Get Configuration Parameters By Job ID")]
	public sealed class GetConfigurationParametersByJobID : IGQIDataSource, IGQIOnInit, IGQIInputArguments
	{
		private readonly GQIStringArgument _jobIdArg = new GQIStringArgument("Job ID") { IsRequired = false };

		private readonly GQIColumn[] _columns = new GQIColumn[]
		{
			new GQIStringColumn("Job Node DOM ID"),		// Used as key to join with the normal nodes GQI query
			new GQIStringColumn("Notes"),
		};

		private DomHelper jobHelper;
		private JobsInstance _job;

		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			// Initialize the data source
			// See: https://aka.dataminer.services/igqioninit-oninit
			jobHelper = new DomHelper(args.DMS.SendMessages, SlcWorkflowIds.ModuleId);
			return default;
		}

		public GQIArgument[] GetInputArguments()
		{
			// Define data source input arguments
			// See: https://aka.dataminer.services/igqiinputarguments-getinputarguments
			return new GQIArgument[]
			{
				_jobIdArg,
			};
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			// Process input argument values
			// See: https://aka.dataminer.services/igqiinputarguments-onargumentsprocessed
			if (!args.TryGetArgumentValue(_jobIdArg, out var _jobId))
			{
				_job = default;
				return default;
			}

			try
			{
				var filter = new ANDFilterElement<DomInstance>(
					DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.Jobs.Id),
					DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobInfo.JobID).Equal(_jobId));
				var jobInstance = jobHelper.DomInstances.Read(filter).SingleOrDefault();
				_job = new JobsInstance(jobInstance);
			}
			catch (Exception)
			{
				_job = default;
			}

			return default;
		}

		public GQIColumn[] GetColumns()
		{
			// Define data source columns
			// See: https://aka.dataminer.services/igqidatasource-getcolumns
			return _columns;
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			// Define data source rows
			// See: https://aka.dataminer.services/igqidatasource-getnextpage
			if (_job == default)
			{
				return new GQIPage(Array.Empty<GQIRow>())
				{
					HasNextPage = false,
				};
			}

			var rows = _job.Nodes.Select(CreateRow).ToArray();
			return new GQIPage(rows)
			{
				HasNextPage = false,
			};
		}

		private static GQIRow CreateRow(NodesSection node)
		{
			try
			{
				var json = JObject.Parse(node.ConfigurationParameters);
				return new GQIRow(new[]
				{
					new GQICell { Value = Convert.ToString(node.ID.Id) },
					new GQICell { Value = json["notes"].Value<string>() },
				});
			}
			catch (Exception)
			{
				return new GQIRow(new[]
				{
					new GQICell { Value = Convert.ToString(node.ID.Id) },
					new GQICell { Value = String.Empty },
				});
			}
		}
	}
}
