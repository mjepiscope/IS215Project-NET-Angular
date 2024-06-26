import { Component, ElementRef, ViewChild } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NgxSpinnerService } from 'ngx-spinner';
import { firstValueFrom } from 'rxjs';

import { AwsService } from './services/aws.service';
import { UploadImageResponse } from './models/upload-image-response';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {

  @ViewChild('articleElement', { read: ElementRef })
  articleElement!: ElementRef<HTMLElement>;

  file!: File;
  imageSrc!: string;
  title: string = " ";
  article: string = " ";
  rekognition_link: string = "";
  loadingText: string = "";
  imageUrl = 'assets/000.png';

  constructor(private service: AwsService, private spinner: NgxSpinnerService, private snack: MatSnackBar) { }

  onImageChange(e: any) {

    if (!e || !e.target || !e.target.files) {
      this.snack.open("File not found!", "Close", { duration: 10000 });
      return;
    }

    this.file = e.target.files[0];

    this.loadingText = "Uploading image...";
    this.spinner.show();

    this.service.uploadImage(this.file).subscribe({

      next: async (uploadResponse: UploadImageResponse) => {

        this.title = "";
        this.article = "";
        this.rekognition_link = ""

        if (!uploadResponse.isSuccess) {
          let errorMessage = uploadResponse.errorMessage ?? "Cannot upload image to S3!";
          this.snack.open(errorMessage, "Close", { duration: 10000 });
          this.spinner.hide();

          return;
        }

        // Show preview only when the image has been uploaded
        this.imageSrc = URL.createObjectURL(this.file);
        this.loadingText = "Retrieving content...";

        let i = 0;
        let isSuccess = false;

        // 30 retries
        while (i < 30) {

          if (i > 10) {
            this.loadingText = "Please wait...";
          }

          try {
            let response = await firstValueFrom(this.service.getGeneratedContent(uploadResponse.timestamp));

            isSuccess = true;

            this.title = response.title;
            this.article = response.article;

            let parsedJson = JSON.parse(response.rekognition_link);
            this.rekognition_link = JSON.stringify(parsedJson, undefined, 2);

            this.snack.open("Facial analysis from uploaded image was generated successfully. Click image field to upload new image.", "Close", { duration: 10000 });
            this.scrollToArticleElement();

            break;
          }
          catch (e) {
            i++;
            // Try move waiting time here before trying again
            await this.delay(250);
          }
        }

        if (!isSuccess) {
          const snackBarRef = this.snack.open("Cannot generate content! Please upload a different photo.", "Close", { duration: 10000 });
          snackBarRef.afterDismissed().subscribe(() => {
            window.location.reload();
          });        
        }

        this.spinner.hide();
      }

      , error: (e) => {
        this.spinner.hide();
        const snackBarRef = this.snack.open("Cannot upload image to S3! Try again!", "Close", { duration: 10000 });
        snackBarRef.afterDismissed().subscribe(() => {
          window.location.reload();
        });
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

  scrollToArticleElement() {
    this.articleElement.nativeElement.scrollIntoView({ behavior: 'smooth' });
  }

  delay(ms: number) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}
