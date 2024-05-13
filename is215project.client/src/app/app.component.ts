import { Component } from '@angular/core';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {

  imageSrc!: string;

  onImageChange(event: any) {
    if (event.target.files && event.target.files[0]) {
      this.imageSrc = URL.createObjectURL(event.target.files[0]);
    }
  }

}
