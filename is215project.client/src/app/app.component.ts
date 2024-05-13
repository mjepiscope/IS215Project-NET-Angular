import { Component } from '@angular/core';
import { AwsService } from './services/aws.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {

  imageSrc!: string;
  article: string = "Generated text here...";

  constructor(private service: AwsService) { }

  onImageChange(event: any) {

    if (event.target.files && event.target.files[0]) {
      this.imageSrc = URL.createObjectURL(event.target.files[0]);
    }

    //TODO read image content here (check correct data type)
    let testImage = "";

    this.service.generateContentFromImage(testImage).subscribe({

      next: (a: string) => {
        this.article = a;
      }

      , error: (e) => {
        console.log(e);
      }

    });

  }

}
