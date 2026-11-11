import { toObservable } from '@angular/core/rxjs-interop';
import { inject } from '@angular/core';
import { CanActivateFn, Router, Routes } from '@angular/router';
import { filter, map, take } from 'rxjs';
import { AuthService } from './auth.service';
import { InformationContent, InformationPage } from './information-page';
import { LandingPage } from './landing-page';
import { Workspace } from './workspace';

const aboutContent: InformationContent = {
  eyebrow: 'About CareerGoat',
  title: 'Your experience, in the right light.',
  intro:
    'CareerGoat is being built to help graduates and junior tech applicants connect what they have done to what a role actually needs, without pretending to be someone they are not.',
  sections: [
    {
      heading: 'Honest tailoring',
      body: 'The goal is to make relevant skills and outcomes easier to see, not to stuff a CV with keywords or invent experience.',
      status: 'OUR APPROACH',
    },
    {
      heading: 'One job at a time',
      body: 'A focused role-by-role workflow is designed to turn a job description into useful application and interview preparation.',
      status: 'THE PRODUCT',
    },
    {
      heading: 'Privacy by design',
      body: 'CVs and job descriptions can be sensitive. The product is being developed with bounded processing, clear consent, and user-initiated deletion in mind.',
      status: 'A CORE PRINCIPLE',
    },
  ],
};

const guideContent: InformationContent = {
  eyebrow: 'The guide',
  title: 'One job at a time. A clearer next step.',
  intro:
    'CareerGoat is being built as a simple loop from a target role to a more confident application. Here is what is available today and what is planned next.',
  sections: [
    {
      heading: 'Create your workspace',
      body: 'Sign in, save your profile, and add target roles with their job descriptions. This is the current workspace functionality.',
      status: 'AVAILABLE NOW',
      points: ['Create an account or sign in with Google', 'Save target roles to your account'],
    },
    {
      heading: 'Tailor with evidence',
      body: 'CV upload, role matching, suggested bullet revisions, and skill-gap analysis are planned for Phase 3. They are not available in the current app yet.',
      status: 'PLANNED - PHASE 3',
      points: ['PDF upload with size and page limits', 'Review and save a structured analysis'],
    },
    {
      heading: 'Practice the conversation',
      body: 'Interview practice with a saved transcript and debrief is planned for Phase 4, after the analysis workflow is in place.',
      status: 'PLANNED - PHASE 4',
      points: ['Practice with one interviewer persona', 'Review feedback and next steps'],
    },
  ],
};

const contactContent: InformationContent = {
  eyebrow: 'Contact',
  title: 'We are building a better way to your next role.',
  intro:
    'A support contact has not been published yet. We will add a real contact method here when one is ready.',
  sections: [
    {
      heading: 'Coming soon',
      body: 'Until a support channel is confirmed, please do not send CVs or other personal information through this page.',
      status: 'CONTACT DETAILS NOT YET AVAILABLE',
    },
  ],
};

const signedInGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return toObservable(auth.ready).pipe(
    filter(Boolean),
    take(1),
    map(() => (auth.session() ? true : router.createUrlTree(['/auth']))),
  );
};

const signedOutGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return toObservable(auth.ready).pipe(
    filter(Boolean),
    take(1),
    map(() => (auth.session() ? router.createUrlTree(['/app']) : true)),
  );
};

export const routes: Routes = [
  { path: '', component: LandingPage },
  { path: 'about', component: InformationPage, data: { content: aboutContent } },
  { path: 'guide', component: InformationPage, data: { content: guideContent } },
  { path: 'contact', component: InformationPage, data: { content: contactContent } },
  { path: 'auth', component: Workspace, canActivate: [signedOutGuard] },
  { path: 'app', component: Workspace, canActivate: [signedInGuard] },
  { path: '**', redirectTo: '' },
];
