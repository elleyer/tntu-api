﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Admin.Database;
using Admin.Exceptions;
using Admin.Helpers.Extensions;
using Admin.Models.Requests;
using Admin.Models.Scheduler;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Admin.Controllers.API
{
    //[Authorize]
    [ApiController]
    [Route("api/scheduler")]
    public sealed class SchedulerController : ControllerBase
    {
        private readonly ApplicationContext _db;

        public SchedulerController(ApplicationContext facultyContext)
        {
            _db = facultyContext;
        }

        [AllowAnonymous]
        [Route("get")]
        public async Task<ActionResult<SchedulerModel>> GetByFacultyAndGroup(string faculty, int spec, 
            string gname, int gcode, int scode)
        {
            try
            {
                var subGroup = await _db.Faculties.GetSubGroup(faculty, spec,
                    gname, gcode, scode);

                var scheduler = subGroup.Scheduler;
                return scheduler;
            }
            
            catch (NotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
        
        [Authorize]
        [HttpPost("day/create")]
        public async Task<IActionResult> CreateNewDay([FromBody] AddSchedulerDayRequest request)
        {
            try
            {
                var subGroup = await _db.Faculties.GetSubGroup(request.FacultyName,
                    request.SpecialityCode, request.GroupName, request.GroupCode,
                    request.SubgroupCode);

                subGroup?.Scheduler.SchedulerDays.CreateNew(request.WeekDay);

                await _db.SaveChangesAsync();

                return Ok($"{request.WeekDay.ToString()} scheduler for id {subGroup?.Id} has been created");
            }

            catch (AlreadyExistsException e)
            {
                return Conflict(e.Message);
            }

            catch (NotFoundException e)
            {
                return NotFound(e.Message);
            }
        }
        
        [Authorize]
        [HttpPost("day/subjects/add")]
        public async Task<IActionResult> AddSubject([FromBody] AddSchedulerSubjectRequest request)
        {
            /*if (User.IsInRole(Role.ADMIN) || User.IsInRole(Role.MOD))
                return Forbid();*/
            try
            {
                var faculty = await _db.Faculties.GetFaculty(request.FacultyName);

                var speciality = await faculty.GetSpeciality(request.SpecialityCode);

                var group = await speciality.GetGroup(request.GroupName, request.GroupCode);

                var subGroup = await group.GetSubGroup(request.SubgroupCode);

                var schedulerDay = subGroup?.Scheduler.SchedulerDays.FirstOrDefault(
                    x => x.ScheduleWeekDay == request.WeekDay);

                schedulerDay?.ClassSubjects.CreateNew(request.ClassSubject);

                await _db.SaveChangesAsync();

                return Ok($"{request.ClassSubject.Name} subject has been created on {faculty.NameEn} => " +
                          $"{group.NameUa}" +
                          $"-{group.Code} ({subGroup?.Code} sub)");
            }

            catch (AlreadyExistsException e)
            {
                return Conflict(e.Message);
            }

            catch (NotFoundException e)
            {
                return NotFound(e.Message);
            }
        }
    }
}