﻿/*
 * FocusEV.OCPP - https://github.com/dallmann-consulting/FocusEV.OCPP
 * Copyright (C) 2020-2021 dallmann consulting GmbH.
 * All Rights Reserved.
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FocusEV.OCPP.Server.Messages_OCPP20
{
#pragma warning disable // Disable all warnings

        [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.3.1.0 (Newtonsoft.Json v9.0.0.0)")]
        public partial class NotifyEVChargingScheduleRequest
    {
            [Newtonsoft.Json.JsonProperty("customData", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public CustomDataType CustomData { get; set; }

            /// <summary>Periods contained in the charging profile are relative to this point in time.
            /// </summary>
            [Newtonsoft.Json.JsonProperty("timeBase", Required = Newtonsoft.Json.Required.Always)]
            [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
            public System.DateTimeOffset TimeBase { get; set; }

            [Newtonsoft.Json.JsonProperty("chargingSchedule", Required = Newtonsoft.Json.Required.Always)]
            [System.ComponentModel.DataAnnotations.Required]
            public ChargingScheduleType ChargingSchedule { get; set; } = new ChargingScheduleType();

            /// <summary>The charging schedule contained in this notification applies to an EVSE. EvseId must be &amp;gt; 0.
            /// </summary>
            [Newtonsoft.Json.JsonProperty("evseId", Required = Newtonsoft.Json.Required.Always)]
            public int EvseId { get; set; }
        }
    }