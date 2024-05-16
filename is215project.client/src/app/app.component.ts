import { Component } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NgxSpinnerService } from 'ngx-spinner';
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
  title: string = " ";
  article: string = " ";
  rekognition_link: string = " ";

  constructor(private service: AwsService, private spinner: NgxSpinnerService, private snack: MatSnackBar) { }

  onImageChange(e: any) {

    if (!e || !e.target || !e.target.files) {
      this.snack.open("File not found!", "Close", { duration: 10000 });
      return;
    }

    this.file = e.target.files[0];
    this.imageSrc = URL.createObjectURL(this.file);

    this.spinner.show();

    this.service.uploadImage(this.file).subscribe({

      next: async (timestamp: number) => {

        let i = 0;
        let n = false;

        // 30 retries
        while (i < 30) {

          try {
            const response = await firstValueFrom(this.service.getGeneratedContent(timestamp));
            this.title = response.title;
            this.article = response.article;
            //this.rekognition_link = JSON.parse(response.rekognition_link);
            this.rekognition_link = " ";
            n = true;
            break;
          }
          catch (e) {
            console.log(e);
            i++;
            // Try move waiting time here before trying again
            await this.delay(2000);
          }
        }

        if (!n) {
          this.snack.open("Cannot generate content!", "Close", { duration: 10000 });
        }

        this.spinner.hide();
      }

      , error: (e) => {
        this.spinner.hide();
        this.snack.open("Cannot upload image to S3!", "Close", { duration: 10000 });
      }

    });

  }

  openRekognitionLink() {
    const newWindow = window.open('', '_blank');
    const htmlContent = `
      <html>
      <head>
        <title>Amazon Rekognition Results</title>
        <style>
          body {
            font-family: Arial, sans-serif;
            padding: 20px;
          }
        </style>
      </head>
      <body>
        <h1>Amazon Rekognition Results</h1>
        <pre>${this.rekognition_link}</pre>
      </body>
      </html>
    `;
    newWindow?.document.write(htmlContent);
    newWindow?.document.close();
  }

 // fetchS3ObjectUrl(timestamp: number) {
 //   this.service.getS3ObjectUrl(timestamp).subscribe({
 //     next: url => {
 //       this.rekognition_link = url;
 //     },
 //     error: error => {
 //       console.error('Error fetching S3 object URL:', error);
 //     }
 //   });
 // }

  delay(ms: number) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}
