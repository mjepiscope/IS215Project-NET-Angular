import { Component } from '@angular/core';
import { AwsService } from '../../services/aws.service';

@Component({
  selector: 'app-test',
    templateUrl: './test.component.html'
})
export class TestComponent {

  result!: boolean;

  constructor(service: AwsService) {

    service.testConnection().subscribe({

      next: (r) => this.result = r

      , error: (e) => {
        this.result = false;
        console.log(e);
      }

    });

  }

}
