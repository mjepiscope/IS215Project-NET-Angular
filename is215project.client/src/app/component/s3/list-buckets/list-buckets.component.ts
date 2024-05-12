import { Component } from '@angular/core';
import { S3Bucket } from '../../../models/s3-bucket';
import { AwsService } from '../../../services/aws.service';

@Component({
  selector: 'app-list-buckets',
  templateUrl: './list-buckets.component.html'
})
export class ListBucketsComponent {

  buckets!: S3Bucket[];

  constructor(service: AwsService) {
    service.getBuckets().subscribe({
      next: (buckets) => this.buckets = buckets
      , error: (e) => {
        this.buckets = [];
        console.log(e);
      }
    });
  }

}
