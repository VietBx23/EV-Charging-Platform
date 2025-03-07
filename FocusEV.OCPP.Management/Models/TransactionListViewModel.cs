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

using FocusEV.OCPP.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FocusEV.OCPP.Management.Models
{
    public class TransactionListViewModel
    {
        public List<ChargePoint> ChargePoints { get; set; }

        public List<ConnectorStatus> ConnectorStatuses { get; set; }

        public Dictionary<string, ChargeTag> ChargeTags { get; set; }

        public string CurrentChargePointId { get; set; }

        public int CurrentConnectorId { get; set; }

        //public string CurrentConnectorName { get; set; }

        public List<Transaction> Transactions { get; set; }

        public int Timespan { get; set; }

    }
}
