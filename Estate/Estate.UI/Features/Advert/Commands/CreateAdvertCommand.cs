﻿using AutoMapper;
using DataAccessLayer.Abstract;
using Estate.UI.Models;

using MediatR;
using System.ComponentModel.DataAnnotations.Schema;

namespace Estate.UI.Features.Advert.Commands
{
    public class CreateAdvertCommand:IRequest<AdvertModel>
    {
        public int AdvertId { get; set; }

        public string AdvertTitle { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public string Garage { get; set; }
        public bool Garden { get; set; }
        public bool Fireplace { get; set; }
        public bool Furniture { get; set; }
        public bool Pool { get; set; }
        public bool Teras { get; set; }
        public bool AirCordinator { get; set; }
        public int NumberOfooms { get; set; }
        public int BathroomNumbers { get; set; }
        public bool Credit { get; set; }
        public int Area { get; set; }
        public DateTime AdvertDate { get; set; } = DateTime.Now;
        public int Floor { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public int CityId { get; set; }
        public int DistrictId { get; set; }
        public int NeighbourhoodId { get; set; }
        public int SituationId { get; set; }
        public int TypeId { get; set; }
        public string UserAdminId { get; set; }
        public bool Status { get; set; } = true;

        [NotMapped]
        public IEnumerable<IFormFile> Image { get; set; }
    }

    public class CreateAdvertCommandHandle : IRequestHandler<CreateAdvertCommand, AdvertModel>
    {
        private readonly IAdvertRepository repository;

        private readonly IMapper mapper;
    

        public CreateAdvertCommandHandle(IAdvertRepository repository, IMapper mapper)
        {
            this.repository = repository;
            this.mapper = mapper;
           

        }
        public async Task<AdvertModel> Handle(CreateAdvertCommand request, CancellationToken cancellationToken)
        {
            //CategoryValidation validationRules = new CategoryValidation();
            //ValidationResult result = validationRules.Validate(request);

            //if (result.IsValid)
            //{
            //    businessRules.CategoryNameCanNotBeDuplicatedWhenInserted(request.Name);

            //    var category = mapper.Map<Category>(request);
            //    await repository.Add(category);
            //}

            //else { throw new ValidationException(result.Errors); }  

       

            EntityLayer.Entities.Advert advert = mapper.Map<EntityLayer.Entities.Advert>(request);
            EntityLayer.Entities.Advert createAdvert = await repository.AddAsync(advert);

            AdvertModel createdAdvertModel = mapper.Map<AdvertModel>(createAdvert);
            return createdAdvertModel;


        }
    }
}
