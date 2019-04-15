﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voting.Web.Data.Entities;
using Voting.Web.Data.Repositories;
using Voting.Web.Helpers;
using Voting.Web.Models;

namespace Voting.Web.Controllers
{
    public class VotingEventsController : Controller
    {
        private readonly ICandidateRepository candidateRepository;
        private readonly IVotingEventRepository votingEventRepository;
        private readonly IResultRepository resultRepository;

        public VotingEventsController(ICandidateRepository candidateRepository,
            IVotingEventRepository votingEventRepository,
            IResultRepository resultRepository)
        {
            this.candidateRepository = candidateRepository;
            this.votingEventRepository = votingEventRepository;
            this.resultRepository = resultRepository;
        }

        public IActionResult Results()
        {
            var result = this.votingEventRepository.GetAllVotingEvents();
            return View(result);
        }

        public IActionResult Index()
        {
            var result = this.votingEventRepository.GetAllVotingEvents();
            return View(result);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        public IActionResult Details(int? id)
        {
            if (id == null)
            {
                return new NotFoundViewResult("NotFound");
            }

            var votingEvent = this.votingEventRepository.GetVotingEvent(id.Value);

            return View(votingEvent);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var votingEvent = this.votingEventRepository.GetVotingEvent(id.Value);

            if (votingEvent == null)
            {
                return NotFound();
            }

            var view = this.ToVotingEventViewModel(votingEvent);
            return View(view);
        }

        public IActionResult AddCandidate(int? id)
        {
            var votingEvent = this.votingEventRepository.GetVotingEvent(id.Value);

            if (votingEvent == null)
            {
                return NotFound();
            }

            var candidate = new Candidate();
            candidate.VotingEventId = id.Value;

            var view = this.ToCandidateViewModel(candidate);

            return View(view);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(VotingEventViewModel view)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var votingEvent = this.ToVotingEvent(view);
                    await this.votingEventRepository.UpdateAsync(votingEvent);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await this.votingEventRepository.ExistAsync(view.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Details), new { id = view.Id});
            }

            return View(view);
        }

        private VotingEvent ToVotingEvent(VotingEventViewModel viewModel)
        {
            return new VotingEvent()
            {
                Id = viewModel.Id,
                Name = viewModel.Name,
                Description = viewModel.Description,
                Candidates = viewModel.Candidates,
                EndDate = viewModel.EndDate,
                StartDate = viewModel.StartDate
            };
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var votingEvent = await this.votingEventRepository.GetByIdAsync(id.Value);
            await this.votingEventRepository.DeleteAsync(votingEvent);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult ProductNotFound()
        {
            return this.View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VotingEventViewModel view)
        {
            if (ModelState.IsValid)
            {
                var votingEvent = this.ToVotingEvent(view);
                await this.votingEventRepository.CreateAsync(votingEvent);
                return RedirectToAction(nameof(Index));
            }

            return View();
        }

        public async Task<IActionResult> DeleteCandidate(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var candidate = await this.candidateRepository.GetByIdAsync(id.Value);
            await this.candidateRepository.DeleteAsync(candidate);
            var votingEvent = this.votingEventRepository.GetVotingEvent(candidate.VotingEventId);

            return View(nameof(Details), votingEvent);
        }

        public async Task<IActionResult> EditCandidate(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var candidate = await this.candidateRepository.GetByIdAsync(id.Value);

            if (candidate == null)
            {
                return NotFound();
            }

            var viewModel = this.ToCandidateViewModel(candidate);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCandidate(CandidateViewModel view)
        {
            if (ModelState.IsValid)
            {
                var path = view.ImageUrl;

                if (view.ImageFile != null && view.ImageFile.Length > 0)
                {
                    var guid = Guid.NewGuid().ToString();
                    var file = $"{guid}.jpg";

                    path = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot\\images\\Candidates",
                        file);

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await view.ImageFile.CopyToAsync(stream);
                    }

                    path = $"~/images/Candidates/{file}";
                }

                var candidate = this.ToCandidate(view, path);
                await this.candidateRepository.UpdateAsync(candidate);

                return RedirectToAction("Details", new { id = candidate.VotingEventId });
            }

            return View(view);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCandidate(CandidateViewModel view, int? id)
        {
            if (ModelState.IsValid)
            {
                var path = string.Empty;

                if (view.ImageFile != null && view.ImageFile.Length > 0)
                {
                    var guid = Guid.NewGuid().ToString();
                    var file = $"{guid}.jpg";

                    path = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot\\images\\Candidates",
                        file);

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await view.ImageFile.CopyToAsync(stream);
                    }

                    path = $"~/images/Candidates/{file}";
                }

                var candidate = this.ToCandidate(view, path);
                candidate.Id = 0;
                candidate.VotingEventId = id.Value;

                await this.candidateRepository.CreateAsync(candidate);
                var votingEvent = await this.votingEventRepository.GetByIdAsync(id.Value);
                votingEvent.Candidates.Append(candidate);
                await this.votingEventRepository.UpdateAsync(votingEvent);

                return RedirectToAction("Details", new { id = votingEvent.Id });
            }

            return View(view);
        }

        private Candidate ToCandidate(CandidateViewModel view, string path)
        {
            return new Candidate
            {
                Id = view.Id,
                ImageUrl = path,
                Proposal = view.Proposal,
                Name = view.Name,
                VotingEventId = view.VotingEventId
            };
        }

        private CandidateViewModel ToCandidateViewModel(Candidate candidate)
        {
            return new CandidateViewModel
            {
                Id = candidate.Id,
                ImageUrl = candidate.ImageUrl,
                Proposal = candidate.Proposal,
                Name = candidate.Name,
                VotingEventId = candidate.VotingEventId
            };
        }

        private VotingEventViewModel ToVotingEventViewModel(VotingEvent votingEvent)
        {
            return new VotingEventViewModel
            {
                Id = votingEvent.Id,
                Candidates = votingEvent.Candidates,
                Description = votingEvent.Description,
                EndDate = votingEvent.EndDate,
                StartDate = votingEvent.StartDate,
                Name = votingEvent.Name
            };
        }

        public IActionResult ResultDetail(int? id)
        {
            if (id == null)
            {
                return new NotFoundViewResult("NotFound");
            }

            var votingEvent = this.votingEventRepository.GetVotingEvent(id.Value);

            return View(votingEvent);
        }
    }
}