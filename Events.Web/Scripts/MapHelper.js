function initialize() {
    var lat = document.getElementById('Latitude').value;
    var lng = document.getElementById('Longitude').value;
    var title = document.getElementById('Title').innerHTML;

    if (lat.length > 0 && lng.length > 0) {
        var map = new google.maps.Map(document.getElementById("map"), { zoom: 14, center: new google.maps.LatLng(lat, lng), mapTypeId: google.maps.MapTypeId['ROADMAP'] });
        var marker = new google.maps.Marker({ position: new google.maps.LatLng(lat, lng), map: map, title: title });
    }
}