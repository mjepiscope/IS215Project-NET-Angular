import { Component } from '@angular/core';
import { AwsService } from './services/aws.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {

  file!: File;
  imageSrc!: string;
  article: string = "Generated text here...";

  constructor(private service: AwsService) { }

  onImageChange(e: any) {

    if (!e || !e.target || !e.target.files) {
      //TODO Show Error
      return;
    }

    this.file = e.target.files[0];
    this.imageSrc = URL.createObjectURL(this.file);

    this.service.generateContentFromImage(this.file).subscribe({

      next: (a: string) => {
        this.article = a;
      }

      , error: (e) => {
        console.log(e);
      }

    });

  }

}
