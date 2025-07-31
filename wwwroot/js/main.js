// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener('DOMContentLoaded',()=>{
    new Swiper('.featured-swiper',{
      slidesPerView:3,
      spaceBetween:24,
      navigation:{
        nextEl:'.featured-swiper .swiper-button-next',
        prevEl:'.featured-swiper .swiper-button-prev'
      },
      breakpoints:{
        0:{slidesPerView:1},
        768:{slidesPerView:2},
        1200:{slidesPerView:3}
      }
    });
  });
document.querySelectorAll('.tab').forEach(btn=>{
  btn.addEventListener('click',()=>{
    document.querySelectorAll('.tab').forEach(t=>t.classList.remove('active'));
    document.querySelectorAll('.tab-content').forEach(c=>c.classList.add('hidden'));
    btn.classList.add('active');
    document.getElementById(btn.dataset.tab).classList.remove('hidden');
  });
});

  