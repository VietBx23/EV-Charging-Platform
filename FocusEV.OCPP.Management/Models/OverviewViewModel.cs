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

namespace FocusEV.OCPP.Management.Models
{
    public class OverviewViewModel
    {

        /// <summary>
        /// List of chargepoints with status information
        /// </summary>
        public List<ChargePointsOverviewViewModel> ChargePoints { get; set; }

        /// <summary>
        /// Does the status contain live information from the OCPP.Server?
        /// </summary>
        public bool ServerConnection { get; set; }
    }
}
