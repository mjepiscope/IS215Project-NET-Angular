function previewImage(event) {
  const file = event.target.files[0];
  if (file) {
    const reader = new FileReader();
    reader.onload = function () {
      const imageSrc = reader.result;
      const uploadContainer = document.getElementById('background-container');
      uploadContainer.style.backgroundImage = `url(${imageSrc})`;
    }
    reader.readAsDataURL(file);
  }
}
