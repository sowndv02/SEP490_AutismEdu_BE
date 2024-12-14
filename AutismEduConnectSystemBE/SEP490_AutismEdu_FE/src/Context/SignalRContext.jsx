import * as signalR from '@microsoft/signalr';
import Cookies from 'js-cookie';
import { createContext, useEffect, useState } from 'react';
import { useSelector } from 'react-redux';
import { tutorInfor } from '~/redux/features/tutorSlice';
import { userInfor } from '~/redux/features/userSlice';
export const SignalRContext = createContext();

export const SignalRProvider = ({ children }) => {
    const [connection, setConnection] = useState(null);
    const tutorInfo = useSelector(tutorInfor);
    const parentInfo = useSelector(userInfor);
    const [openMessage, setOpenMessage] = useState(false);
    const [currentChat, setCurrentChat] = useState(null);
    const [conversations, setConversations] = useState([]);
    useEffect(() => {
        let userId = "";
        if (tutorInfo) {
            userId = tutorInfo.id;
        }
        if (parentInfo) {
            userId = parentInfo.id;
        }
        if (userId) {
            const newConnection = new signalR.HubConnectionBuilder()
                .withUrl(`${import.meta.env.VITE_BASE_URL}hub/notifications?userId=${userId}`)
                .withAutomaticReconnect()
                .configureLogging(signalR.LogLevel.Information)
                .build();
            setConnection(newConnection);
            newConnection.start()
                .then(() => {
                    console.log("SignalR connected");
                })
                .catch((err) => console.error("SignalR connection failed:", err));
            return () => {
                newConnection.stop().then(() => console.log("SignalR disconnected"));
            };
        }
    }, [tutorInfo, parentInfo]);
    return (
        <SignalRContext.Provider value={{
            connection,
            openMessage,
            setOpenMessage,
            conversations,
            setConversations,
            currentChat,
            setCurrentChat,
        }}>
            {children}
        </SignalRContext.Provider>
    );
};
