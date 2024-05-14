import { Component } from '@angular/core';
import { firstValueFrom } from 'rxjs';
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

    this.service.uploadImage(this.file).subscribe({

      next: async (filenameWithTimestamp: string) => {
        
        let expectedFilename = this.getExpectedFilename(filenameWithTimestamp);

        let i = 0;

        // 30 retries
        while (i < 30) {

          try {
            this.article = await firstValueFrom(this.service.getGeneratedContent(expectedFilename));
            break;
          }
          catch (e) {
            console.log(e);
            i++;
          }

          // wait 2 sec before trying again
          this.delay(2000);
        }

      }

      , error: (e) => {
        console.log(e);
      }

    });

  }

  getExpectedFilename(filenameWithTimestamp: string) {
    let pos = filenameWithTimestamp.lastIndexOf(".");

    return filenameWithTimestamp.substring(0, pos < 0 ? filenameWithTimestamp.length : pos) + ".txt";
  }

  delay(ms: number) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}
