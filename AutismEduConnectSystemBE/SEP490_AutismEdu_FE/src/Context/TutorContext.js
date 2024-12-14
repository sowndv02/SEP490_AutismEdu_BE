import { createContext, useContext } from 'react';

const TutorContext = createContext(null);

export const useTutorContext = () => {
    return useContext(TutorContext);
};

export default TutorContext;