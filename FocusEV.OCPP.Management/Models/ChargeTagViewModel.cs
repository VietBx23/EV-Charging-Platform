/*
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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FocusEV.OCPP.Management.Models
{
    public class ChargeTagViewModel
    {
        public List<ChargeTag> ChargeTags { get; set; }

        public string CurrentTagId { get; set; }


        [Required, StringLength(50)]
        public string TagId { get; set; }

        [Required, StringLength(200)]
        public string TagName { get; set; }

        [StringLength(50)]
        public string ParentTagId { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        public bool Blocked { get; set; }

        [StringLength(50)]
        public string SideId { get; set; } //bổ sung Terry: 01/10

        public int ChargeStationId { get; set; } //bổ sung Terry: 01/10

        [StringLength(200)]
        public string TagDescription { get; set; } //bổ sung Terry: 01/10

        [Required, StringLength(50)]
        public string TagType { get; set; }  //bổ sung Terry: 28/09

        [Required, StringLength(50)]
        public string TagState { get; set; }  //bổ sung Terry: 28/09

        [DataType(DataType.Date)]
        public DateTime? CreateDate { get; set; }  //bổ sung Terry: 28/09


    }
}
