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
  article: string = "Generated text here...";

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
            this.article = await firstValueFrom(this.service.getGeneratedContent(timestamp));
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

  delay(ms: number) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}
