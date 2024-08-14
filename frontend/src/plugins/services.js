import axios from '~/plugins/axios';
import { initializeService } from '~/services/BaseService';
import { AuthenticationAPI } from '~/services/AuthenticationAPI';

// Initialize the BaseService with the axios instance and API prefix
initializeService(axios, '/api');

const services = {
  AuthenticationAPI,
};

export default services;