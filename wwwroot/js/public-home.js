(function(){
  document.addEventListener('DOMContentLoaded', function(){
    var form = document.getElementById('bookingForm');
    var msg = document.getElementById('bookingMsg');
    
    // 1. Header scroll micro-interaction
    window.addEventListener('scroll', function() {
        var nav = document.querySelector('nav');
        if (nav) {
            if (window.scrollY > 50) {
                nav.classList.add('py-2');
                nav.classList.remove('py-4');
            } else {
                nav.classList.add('py-4');
                nav.classList.remove('py-2');
            }
        }
    });

    if(!form) return;

    // 2. Booking form submit AJAX integration
    form.addEventListener('submit', function(e){
      e.preventDefault();
      
      var btn = form.querySelector('button[type="submit"]');
      var originalContent = btn ? btn.innerHTML : 'Gửi yêu cầu';

      // Transition state: Loading spinner & disabled
      if (btn) {
          btn.disabled = true;
          btn.innerHTML = '<span>Đang xử lý...</span><span class="material-symbols-outlined animate-spin ml-2">sync</span>';
          btn.classList.add('opacity-80');
      }
      
      if (msg) {
          msg.className = "md:col-span-2 mt-4 text-center font-bold text-primary";
          msg.innerText = "";
      }

      var data = {};
      new FormData(form).forEach(function(v,k){ data[k]=v; });

      // Convert datetime-local to ISO format for C# model binder if provided
      if(data.AppointmentTime){
        try{ data.AppointmentTime = new Date(data.AppointmentTime).toISOString(); }catch(err){}
      }

      fetch('/Appointments/Create', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
      })
      .then(function(r){ return r.json(); })
      .then(function(j){
        // Restore button state
        if (btn) {
          btn.disabled = false;
          btn.innerHTML = originalContent;
          btn.classList.remove('opacity-80');
        }
        
        if(j && j.success){
          if (msg) {
              msg.className = "md:col-span-2 mt-4 text-center font-semibold text-emerald-400 bg-emerald-500/10 py-3 px-4 rounded-xl border border-emerald-500/20 shadow-md";
              msg.innerText = j.message || 'Đặt lịch thành công! Đội ngũ tư vấn sẽ sớm liên hệ.';
          }
          alert(j.message || 'Đặt lịch thành công! Chúng tôi sẽ sớm liên hệ.');
          form.reset();
        } else {
          if (msg) {
              msg.className = "md:col-span-2 mt-4 text-center font-semibold text-rose-400 bg-rose-500/10 py-3 px-4 rounded-xl border border-rose-500/20 shadow-md";
              msg.innerText = j.message || 'Có lỗi xảy ra, vui lòng liên hệ hotline!';
          }
        }
      })
      .catch(function(err){
        // Restore button state
        if (btn) {
          btn.disabled = false;
          btn.innerHTML = originalContent;
          btn.classList.remove('opacity-80');
        }
        
        if (msg) {
          msg.className = "md:col-span-2 mt-4 text-center font-semibold text-rose-400 bg-rose-500/10 py-3 px-4 rounded-xl border border-rose-500/20 shadow-md";
          msg.innerText = 'Lỗi kết nối: ' + err.message;
        }
      });
    });
  });
})();
