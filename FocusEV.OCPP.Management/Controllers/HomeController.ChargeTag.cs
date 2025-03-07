﻿
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using FocusEV.OCPP.Database;
using FocusEV.OCPP.Management.Models;

namespace FocusEV.OCPP.Management.Controllers
{
    public partial class HomeController : BaseController
    {
        [Authorize]
        public IActionResult ChargeTag(string Id, ChargeTagViewModel ctvm)
        {
            try
            {
                if (User != null && !User.IsInRole(Constants.AdminRoleName))
                {
                    Logger.LogWarning("ChargeTag: Request by non-administrator: {0}", User?.Identity?.Name);
                    TempData["ErrMsgKey"] = "AccessDenied";
                    return RedirectToAction("Error", new { Id = "" });
                }

                ViewBag.DatePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                ViewBag.Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                ctvm.CurrentTagId = Id;

                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {
                    Logger.LogTrace("ChargeTag: Loading charge tags...");
                    List<ChargeTag> dbChargeTags = dbContext.ChargeTags.ToList<ChargeTag>();
                    Logger.LogInformation("ChargeTag: Found {0} charge tags", dbChargeTags.Count);

                    ChargeTag currentChargeTag = null;
                    if (!string.IsNullOrEmpty(Id))
                    {
                        foreach (ChargeTag tag in dbChargeTags)
                        {
                            if (tag.TagId.Equals(Id, StringComparison.InvariantCultureIgnoreCase))
                            {
                                currentChargeTag = tag;
                                Logger.LogTrace("ChargeTag: Current charge tag: {0} / {1}", tag.TagId, tag.TagName);
                                break;
                            }
                        }
                    }

                    if (Request.Method == "POST")
                    {
                        string errorMsg = null;

                        if (Id == "@")
                        {
                            Logger.LogTrace("ChargeTag: Creating new charge tag...");

                            // Create new tag
                            if (string.IsNullOrWhiteSpace(ctvm.TagId))
                            {
                                errorMsg = _localizer["ChargeTagIdRequired"].Value;
                                Logger.LogInformation("ChargeTag: New => no charge tag ID entered");
                            }

                            if (string.IsNullOrEmpty(errorMsg))
                            {
                                // check if duplicate
                                foreach (ChargeTag tag in dbChargeTags)
                                {
                                    if (tag.TagId.Equals(ctvm.TagId, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        // tag-id already exists
                                        errorMsg = _localizer["ChargeTagIdExists"].Value;
                                        Logger.LogInformation("ChargeTag: New => charge tag ID already exists: {0}", ctvm.TagId);
                                        break;
                                    }
                                }
                            }

                            if (string.IsNullOrEmpty(errorMsg))
                            {
                                // Save tag in DB
                                ChargeTag newTag = new ChargeTag();
                                newTag.TagId = ctvm.TagId;
                                newTag.TagName = ctvm.TagName;
                                newTag.ParentTagId = ctvm.ParentTagId;
                                newTag.ExpiryDate = ctvm.ExpiryDate;
                                newTag.Blocked = ctvm.Blocked;
                                dbContext.ChargeTags.Add(newTag);
                                dbContext.SaveChanges();
                                Logger.LogInformation("ChargeTag: New => charge tag saved: {0} / {1}", ctvm.TagId, ctvm.TagName);
                            }
                            else
                            {
                                ViewBag.ErrorMsg = errorMsg;
                                return View("ChargeTagDetail", ctvm);
                            }
                        }
                        else if (currentChargeTag.TagId == Id)
                        {
                            // Save existing tag
                            currentChargeTag.TagName = ctvm.TagName;
                            currentChargeTag.ParentTagId = ctvm.ParentTagId;
                            currentChargeTag.ExpiryDate = ctvm.ExpiryDate;
                            currentChargeTag.Blocked = ctvm.Blocked;
                            dbContext.SaveChanges();
                            Logger.LogInformation("ChargeTag: Edit => charge tag saved: {0} / {1}", ctvm.TagId, ctvm.TagName);
                        }

                        return RedirectToAction("ChargeTag", new { Id = "" });
                    }
                    else
                    {
                        // List all charge tags
                        ctvm = new ChargeTagViewModel();
                        ctvm.ChargeTags = dbChargeTags;
                        ctvm.CurrentTagId = Id;

                        if (currentChargeTag != null)
                        {
                            ctvm.TagId = currentChargeTag.TagId;
                            ctvm.TagName = currentChargeTag.TagName;
                            ctvm.ParentTagId = currentChargeTag.ParentTagId;
                            ctvm.ExpiryDate = currentChargeTag.ExpiryDate;
                            ctvm.Blocked = (currentChargeTag.Blocked != null) && currentChargeTag.Blocked.Value;
                        }

                        string viewName = (!string.IsNullOrEmpty(ctvm.TagId) || Id=="@") ? "ChargeTagDetail" : "ChargeTagList";
                        return View(viewName, ctvm);
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "ChargeTag: Error loading charge tags from database");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }
        }
    }
}
