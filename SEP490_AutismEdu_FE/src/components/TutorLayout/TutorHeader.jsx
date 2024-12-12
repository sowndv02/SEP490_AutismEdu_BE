import { Logout } from '@mui/icons-material';
import MenuIcon from '@mui/icons-material/Menu';
import NotificationsActiveIcon from '@mui/icons-material/NotificationsActive';
import { Avatar, Badge, Box, Button, Divider, FormControl, IconButton, InputAdornment, ListItemIcon, Menu, MenuItem, OutlinedInput, Paper, Stack, TextField, Typography } from '@mui/material';
import { deepPurple } from '@mui/material/colors';
import Cookies from "js-cookie";
import { useContext, useEffect, useRef, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { Link, useNavigate } from 'react-router-dom';
import { packagePayment, setPackagePayment } from '~/redux/features/packagePaymentSlice';
import { setTutorInformation, tutorInfor } from '~/redux/features/tutorSlice';
import SearchIcon from '@mui/icons-material/Search';
import KeyboardDoubleArrowUpIcon from '@mui/icons-material/KeyboardDoubleArrowUp';
import PAGES from '~/utils/pages';
import Logo from '../Logo';
import ChatIcon from '@mui/icons-material/Chat';
import dayjs from 'dayjs';
import relativeTime from 'dayjs/plugin/relativeTime';
import { SignalRContext } from '~/Context/SignalRContext';
import services from '~/plugins/services';
import InputEmoji from "react-input-emoji";
import SendIcon from '@mui/icons-material/Send';
import { jwtDecode } from 'jwt-decode';
import CurrencyExchangeIcon from '@mui/icons-material/CurrencyExchange';
import Settings from '@mui/icons-material/Settings';

dayjs.locale('vi');
function TutorHeader({ openMenu, setOpenMenu }) {
    dayjs.extend(relativeTime);
    const nav = useNavigate();
    const tutorInfo = useSelector(tutorInfor);
    const aPackagePayment = useSelector(packagePayment);
    const [accountMenu, setAccountMenu] = useState();
    const dispatch = useDispatch();
    const openAccountMenu = Boolean(accountMenu);
    const [openNotification, setOpenNotification] = useState(false);
    const { connection, openMessage, setOpenMessage, setCurrentChat, currentChat, conversations, setConversations } = useContext(SignalRContext);
    const [notifications, setNotifications] = useState([]);
    const notificationRef = useRef(null);
    const notificationIconRef = useRef(null);
    const [isTrial, setTrial] = useState(true);
    const [unreadNoti, setUnreadNoti] = useState(0);
    const [daysLeft, setDaysLeft] = useState(0);
    const messageIconRef = useRef(null);
    const [text, setText] = useState("");
    const [messages, setMessages] = useState([]);
    const [chatBox, setChatBox] = useState(null);
    const [unreadMessage, setUnreadMessage] = useState(true);
    const [newMessage, setNewMessage] = useState(null);
    const [newNotification, setNewNotification] = useState(null)
    const handleGetCurrentUserPaymentHistory = async () => {
        try {
            await services.PaymentHistoryAPI.getListPaymentHistoryCurrent((res) => {
                dispatch(setPackagePayment(res.result));
            }, (error) => {
                console.log(error);
            });
        } catch (error) {
            console.log(error);
        }
    };

    useEffect(() => {
        handleGetCurrentUserPaymentHistory();
        setMessages([1, 2]);
    }, []);

    useEffect(() => {
        if (openMessage && !currentChat && conversations.length !== 0) {
            setCurrentChat(conversations[0]);
        }
        if (!openMessage && conversations.length !== 0 && currentChat) {
            const filterConversation = conversations.filter((c) => {
                return c.id !== 0
            })
            setConversations([...filterConversation])
            if (currentChat.id === 0) {
                setCurrentChat(filterConversation[0])
            }
        }
    }, [openMessage])
    useEffect(() => {
        const calculateDaysLeft = (endDate) => {
            const currentDate = new Date();
            const end = new Date(endDate);
            const timeDiff = end - currentDate;
            return Math.ceil(timeDiff / (1000 * 60 * 60 * 24));
        };

        if (aPackagePayment?.expirationDate) {
            const expirationDaysLeft = calculateDaysLeft(aPackagePayment.expirationDate);
            setTrial(false);
            setDaysLeft(expirationDaysLeft > 0 ? expirationDaysLeft : 0);
        } else if (tutorInfo?.createdDate) {
            const createdDate = new Date(tutorInfo.createdDate);
            const trialEndDate = new Date(createdDate);
            trialEndDate.setDate(trialEndDate.getDate() + 30);

            const trialDaysLeft = calculateDaysLeft(trialEndDate);
            setTrial(true);
            setDaysLeft(trialDaysLeft > 0 ? trialDaysLeft : 0);
        }
    }, [tutorInfo, aPackagePayment]);

    useEffect(() => {
        const accessToken = Cookies.get("access_token");
        if (!accessToken) {
            nav(PAGES.TUTOR_LOGIN)
        }
        const decodedToken = jwtDecode(accessToken);
        const role = decodedToken['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
        if (role !== "Tutor") {
            nav(PAGES.TUTOR_LOGIN)
        }
        if (tutorInfo) {
            handleGetNotification();
            handleGetConversation();
        }
    }, [tutorInfo])

    useEffect(() => {
        if (currentChat && currentChat?.id !== 0) {
            handleGetMessage(1, 10);
            handleReadMessage();
        } else setMessages([])
        setText("");
    }, [currentChat])
    useEffect(() => {
        if (!connection || !tutorInfo) return;
        connection.on(`Notifications-${tutorInfo.id}`, (notification) => {
            setNewNotification(notification);
        });
        connection.on(`Messages-${tutorInfo.id}`, (message) => {
            setNewMessage(message)
        });
        return () => {
            connection.off(`Notifications-${tutorInfo.id}`);
            connection.off(`Messages-${tutorInfo.id}`);
        };
    }, [connection, tutorInfo]);

    useEffect(() => {
        if (newNotification) {
            setNotifications((preNotifications) => [newNotification, ...preNotifications]);
            setUnreadNoti(unreadNoti + 1)
        }
    }, [newNotification])
    useEffect(() => {
        if (newMessage) {
            if (newMessage.conversation.id === currentChat.id)
                setMessages((preMessages) => [...preMessages, newMessage]);
            const receiveConversation = conversations.find((c) => {
                return c.id === newMessage.conversation.id
            })
            receiveConversation.messages = [newMessage];
            const filtedConversation = conversations.filter((c) => {
                return c.id !== newMessage.conversation.id;
            })

            if (receiveConversation.id !== currentChat.id && receiveConversation.isRead === true) {
                receiveConversation.isRead = false;
                setUnreadMessage(false);
            }
            if (receiveConversation.id === currentChat.id) {
                handleReadMessage();
                if (chatBox) {
                    chatBox.scrollTop = chatBox.scrollHeight;
                }
            }
            setConversations([receiveConversation, ...filtedConversation])
        }
    }, [newMessage])
    const handleGetNotification = async () => {
        try {
            await services.NotificationAPI.getAllPaymentPackage((res) => {
                const filterArr = res.result.result.filter((r, index) => {
                    return index <= 10
                })
                setNotifications([...notifications, ...filterArr]);
                setUnreadNoti(res.result.totalUnRead);
            }, (error) => {
                console.log(error);
            }, {
                pageNumber: notifications.length === 0 ? 1 : 2,
                pageSize: notifications.length <= 10 ? 10 : notifications.length
            })
        } catch (error) {
            console.log(error);
        }
    }

    const handleGetConversation = async () => {
        try {
            await services.ConversationAPI.getConversations((res) => {
                const returnArr = res.result.map((r) => {
                    if (r.messages[0].sender.id === tutorInfo.id) {
                        r.isRead = true;
                    } else {
                        r.isRead = r.messages[0].isRead;
                    }
                    return r;
                })
                returnArr.forEach((r) => {
                    if (r.isRead === false) {
                        setUnreadMessage(false);
                    }
                })
                setConversations(returnArr);
            }, (error) => {
                console.log(error);
            }, {
                pageNumber: 1
            })
        } catch (error) {
            console.log(error);
        }
    }
    const handleGetMessage = async (pageNumber, pageSize) => {
        try {
            await services.MessageAPI.getMessages(currentChat?.id || 0, (res) => {
                setMessages([...res.result.reverse(), ...messages]);
            }, (error) => {
                console.log(error);
            }, {
                pageNumber: pageNumber,
                pageSize: pageSize
            })
        } catch (error) {
            console.log(error);
        }
    }
    const handleReadMessage = async () => {
        try {
            await services.MessageAPI.readMessages(currentChat?.id || 0, {}, (res) => {
                const currentChatBox = conversations.find((c) => {
                    return c.id === currentChat.id
                })
                currentChatBox.isRead = true;
                setUnreadMessage(true);
                conversations.forEach((r) => {
                    if (r.isRead === false) {
                        setUnreadMessage(false);
                    }
                })
                setConversations([...conversations]);
            }, (error) => {
                console.log(error);
            })
        } catch (error) {
            console.log(error);
        }
    }

    const sendMessages = async () => {
        if (text.trim() === "") return;
        try {
            if (currentChat.id !== 0) {
                await services.MessageAPI.sendMessages({
                    conversationId: currentChat.id,
                    content: text.trim(),
                }, (res) => {
                    setMessages([...messages, res.result]);
                    const receiveConversation = conversations.find((c) => {
                        return c.id === res.result.conversation.id
                    })

                    receiveConversation.messages = [res.result];
                    const filtedConversation = conversations.filter((c) => {
                        return c.id !== res.result.conversation.id;
                    })
                    setConversations([receiveConversation, ...filtedConversation])
                    const filterConver = conversations.filter((c) => {
                        return c.id !== receiveConversation.id;
                    })
                    setConversations([receiveConversation, ...filterConver])
                    setText("");
                    if (chatBox) {
                        chatBox.scrollTop = chatBox.scrollHeight;
                    }
                }, (error) => {
                    console.log(error);
                })
            }
            else {
                await services.ConversationAPI.createConversations({
                    receiverId: currentChat.user.id,
                    message: text.trim()
                }, (res) => {
                    const receiveConversation = conversations.find((c) => {
                        return c.id === 0
                    })
                    const filtedConversation = conversations.filter((c) => {
                        return c.id !== 0;
                    })
                    receiveConversation.id = res.result.id
                    receiveConversation.messages = res.result.messages;
                    setCurrentChat(receiveConversation)
                    setConversations([receiveConversation, ...filtedConversation])
                    setText("");
                }, (error) => {
                    console.log(error);
                })
            }
        } catch (error) {
            console.log(error);
        }
    }
    const handleOpenAccountMenu = (event) => {
        setAccountMenu(event.currentTarget);
    };
    const handleCloseAccountMenu = () => {
        setAccountMenu(null);
    };
    const handleOpenMenu = () => {
        setOpenMenu(!openMenu)
    }

    const handleLogout = () => {
        Cookies.remove("access_token");
        Cookies.remove("refresh_token");
        dispatch(setTutorInformation(null));
        dispatch(setPackagePayment(null));
        nav(PAGES.TUTOR_LOGIN)
    };

    const handleClickOutside = (event) => {
        if (
            messageIconRef.current &&
            !messageIconRef.current?.contains(event.target) &&
            !event.target.closest(".MuiIconButton-root")
        ) {
            setOpenMessage(false);
        }
        if (
            notificationRef.current &&
            !notificationRef.current?.contains(event.target) &&
            !notificationIconRef.current?.contains(event.target) &&
            !event.target.closest(".MuiIconButton-root")
        ) {
            setOpenNotification(false);
        }
    };
    useEffect(() => {
        document.addEventListener("mousedown", handleClickOutside);
        return () => {
            document.removeEventListener("mousedown", handleClickOutside);
        };
    }, []);

    const handleReadOne = async (id) => {
        try {
            await services.NotificationAPI.readAPaymentPackage(id, {}, (res) => {
                const selectedNoti = notifications.find((n) => {
                    return n.id === id;
                })
                selectedNoti.isRead = true;
                setNotifications([...notifications]);
                setUnreadNoti(unreadNoti - 1);
            }, (error) => {
                console.log(error);
            })
        } catch (error) {
            console.log(error);
        }
    }
    const handleReadAll = async () => {
        try {
            await services.NotificationAPI.readAllPaymentPackage({}, (res) => {
                notifications.forEach((n) => {
                    n.isRead = true;
                })
                setNotifications([...notifications]);
                setUnreadNoti(0)
            }, (error) => {
                console.log(error);
            })
        } catch (error) {
            console.log(error);
        }
    }

    useEffect(() => {
        if (chatBox && messages.length <= 10) {
            chatBox.scrollTop = chatBox.scrollHeight;
        }
    }, [messages, chatBox]);

    return (
        <Box sx={{
            position: "fixed",
            top: "0",
            width: "100vw",
            zIndex: 100,
            bgcolor: 'white'
        }}>
            <Stack direction='row' sx={{
                justifyContent: "space-between",
                height: "64px",
                alignItems: "center",
                px: "20px"
            }}>
                <Box sx={{ display: "flex", gap: 2 }}>
                    <IconButton onClick={handleOpenMenu}>
                        <MenuIcon />
                    </IconButton>
                    <Logo sizeLogo={30} sizeName={25} />
                </Box>
                <Box sx={{ display: "flex", gap: 2 }}>
                    {(
                        <Box display={'flex'} alignItems={'center'}>
                            <Typography
                                variant="h6"
                                sx={{
                                    color: daysLeft < 10 ? 'red' : 'green',
                                    animation: daysLeft < 10 ? 'blink 1s step-start infinite' : 'none',
                                    '@keyframes blink': {
                                        '50%': { opacity: 0 }
                                    }
                                }}
                            >
                                {!isTrial ? 'Hạn còn lại:' : 'Dùng thử:'} {daysLeft} ngày
                            </Typography>
                        </Box>
                    )}
                    <Button startIcon={<KeyboardDoubleArrowUpIcon />} onClick={() => nav(PAGES.PAYMENT_PACKAGE)} variant='contained' size='small' sx={{
                        width: "130px", bgcolor: '#16ab65',
                        '&:hover': {
                            bgcolor: '#128a51'
                        }
                    }}>
                        Nâng cấp
                    </Button>
                    <Box sx={{ position: "relative" }}>
                        <IconButton sx={{ color: "#c58ee5" }}
                            onClick={(e) => {
                                e.stopPropagation();
                                setOpenMessage(!openMessage);
                                setOpenNotification(false);
                            }}>
                            <Badge variant="dot" invisible={unreadMessage} color="primary">
                                <ChatIcon />
                            </Badge>
                        </IconButton>
                        {
                            openMessage && (
                                <Paper variant='elevation' sx={{
                                    position: "absolute", top: "50px",
                                    right: "0px", width: "1000px", bgcolor: "#F3E8FF",
                                    p: 2,
                                    height: "85vh"
                                }} ref={messageIconRef}>
                                    {
                                        conversations.length !== 0 ? (
                                            <Stack direction='row' sx={{ height: "100%" }} gap={1}>
                                                <Box sx={{ width: "35%" }}>
                                                    <Typography variant='h4'>Đoạn chat</Typography>
                                                    <Box sx={{
                                                        overflow: "hidden", height: "80%",
                                                        "&:hover": {
                                                            overflow: "auto"
                                                        },
                                                        mt: 1
                                                    }}>
                                                        {
                                                            conversations && conversations.length !== 0 && conversations.map((c) => {
                                                                return (
                                                                    <Stack key={c.id} direction='row' gap={1} alignItems="center" sx={{
                                                                        py: 2, justifyContent: "space-between",
                                                                        cursor: "pointer",
                                                                        px: 2,
                                                                        bgcolor: c.id === currentChat?.id ? "#F8F0FF" : "",
                                                                        borderRadius: "10px",
                                                                        ":hover": {
                                                                            bgcolor: "#F8F0FF"
                                                                        }
                                                                    }} onClick={() => setCurrentChat(c)}>
                                                                        <Stack direction='row' gap={2} alignItems="center" sx={{ width: "80%" }}>
                                                                            <Avatar alt="Remy Sharp" src={c.user.imageUrl} />
                                                                            <Box sx={{ overflow: "hidden" }}>
                                                                                <Typography fontWeight="bold" color="black">{c.user.fullName}</Typography>
                                                                                <Typography sx={{
                                                                                    display: '-webkit-box',
                                                                                    WebkitLineClamp: 1,
                                                                                    WebkitBoxOrient: 'vertical',
                                                                                    overflow: 'hidden',
                                                                                    textOverflow: 'ellipsis'
                                                                                }}>{c.messages ? c?.messages[0]?.content : ""}</Typography>
                                                                            </Box>
                                                                        </Stack>
                                                                        {
                                                                            !c?.isRead && (
                                                                                <Box sx={{ width: "15px", height: "15px", bgcolor: "blue", borderRadius: "50%" }}>
                                                                                </Box>
                                                                            )
                                                                        }
                                                                    </Stack>
                                                                )
                                                            })
                                                        }
                                                    </Box>
                                                </Box>
                                                <Divider orientation="vertical" flexItem />

                                                <Stack direction='column' sx={{ width: "65%", height: "100%" }}>
                                                    <Stack direction='row' sx={{
                                                        px: 1, alignItems: "center",
                                                        justifyContent: "space-between"
                                                    }}>
                                                        <Stack direction='row' sx={{ gap: 2, alignItems: "center" }}>
                                                            <Avatar alt="Remy Sharp" src={currentChat ? currentChat?.user?.imageUrl : "/"} sx={{
                                                                width: "50px",
                                                                height: "50px"
                                                            }} />
                                                            <Typography variant='h5' sx={{}}>{currentChat ? currentChat?.user?.fullName : "Mất kết nối"}</Typography>
                                                        </Stack>
                                                    </Stack>
                                                    <Divider sx={{ mt: 1 }} />
                                                    <Box style={{
                                                        width: "100%",
                                                        flexGrow: 2,
                                                        overflow: "auto"
                                                    }} ref={setChatBox}>
                                                        {
                                                            messages && messages.length !== 0 && messages.map((m) => {
                                                                if (m.sender?.id === tutorInfo?.id) {
                                                                    return (
                                                                        <Stack key={m.id} direction='row' sx={{
                                                                            justifyContent: "flex-end",
                                                                            mt: 1
                                                                        }}>
                                                                            <Box sx={{
                                                                                bgcolor: "#E0D1FF",
                                                                                p: 2,
                                                                                borderRadius: "15px",
                                                                                maxWidth: "70%"
                                                                            }}>
                                                                                <Typography sx={{ whiteSpace: "normal", wordBreak: "break-word" }}>{m.content}</Typography>
                                                                            </Box>
                                                                        </Stack>
                                                                    )
                                                                } else {
                                                                    return (
                                                                        <Stack direction="row" key={m.id} sx={{ mt: 1 }}>
                                                                            <Box sx={{
                                                                                bgcolor: "#FBF8FF",
                                                                                p: 2,
                                                                                borderRadius: "15px",
                                                                                maxWidth: "70%",
                                                                                flexShrink: 0
                                                                            }}>
                                                                                <Typography sx={{ whiteSpace: "normal", wordBreak: "break-word" }}>{m.content}</Typography>
                                                                            </Box>
                                                                        </Stack>
                                                                    )
                                                                }
                                                            })
                                                        }
                                                    </Box>
                                                    <Divider sx={{ mt: 1 }} />
                                                    <Stack direction='row' justifyContent='space-between' alignItems="center"
                                                        sx={{
                                                            maxHeight: "110px"
                                                        }}>
                                                        <Box flexGrow={2} sx={{ whiteSpace: "normal", wordBreak: "break-word" }}>
                                                            <InputEmoji
                                                                value={text}
                                                                onChange={setText}
                                                                cleanOnEnter
                                                                onKeyDown={(event) => {
                                                                    if (event.key === "Enter") {
                                                                        sendMessages();
                                                                    }
                                                                }}
                                                                placeholder="Nhập tin nhắn vào đây"
                                                            />
                                                        </Box>
                                                        <IconButton onClick={() => sendMessages()} sx={{
                                                            bgcolor: "#c079ea", color: "white",
                                                            ":hover": {
                                                                bgcolor: "#c58ee5"
                                                            }
                                                        }}>
                                                            <SendIcon />
                                                        </IconButton>
                                                    </Stack>
                                                </Stack>
                                            </Stack>
                                        ) : (
                                            <Box sx={{
                                                width: "100%",
                                                height: "100%",
                                                display: "flex",
                                                justifyContent: "center",
                                                alignItems: "center"
                                            }}>
                                                <Typography sx={{ fontSize: "30px" }}>Bạn chưa có cuộc hội thoại nào</Typography>
                                            </Box>
                                        )
                                    }
                                </Paper>
                            )
                        }
                    </Box>
                    <Box sx={{ position: "relative" }}>
                        <IconButton sx={{ color: "#ff7900" }}
                            onClick={(e) => {
                                e.stopPropagation();
                                setOpenMessage(false);
                                setOpenNotification(!openNotification);
                            }}>
                            <Badge badgeContent={unreadNoti} color="primary">
                                <NotificationsActiveIcon />
                            </Badge>
                        </IconButton>
                        {
                            openNotification && (
                                <Paper variant='elevation' sx={{
                                    position: "absolute", top: "50px",
                                    right: "0px", width: "400px", overflow: 'auto', maxHeight: "70vh", bgcolor: "#f1f1f1"
                                }} ref={notificationRef}>
                                    <Typography variant='h4' p={2}>Thông báo</Typography>
                                    <Button sx={{ p: 2 }} onClick={handleReadAll}>Đánh dấu đọc tất cả</Button>
                                    {
                                        notifications.length !== 0 && notifications.map((n) => {
                                            return (
                                                <Link to={n.urlDetail} key={n.id}>
                                                    <Box sx={{
                                                        display: "flex", alignItems: "center", justifyContent: 'space-between',
                                                        p: 2, cursor: "pointer",
                                                        ":hover": {
                                                            bgcolor: "white"
                                                        }
                                                    }} onClick={() => {
                                                        if (!n.isRead)
                                                            handleReadOne(n.id)
                                                    }}>
                                                        <Box sx={{ width: "90%" }}>
                                                            <Typography>
                                                                {n.message}
                                                            </Typography>
                                                            <Typography color={"#556cd6"} fontSize={"12px"}>{dayjs(new Date(n.createdDate)).fromNow()}</Typography>
                                                        </Box>
                                                        {
                                                            !n.isRead && (
                                                                <Box sx={{ borderRadius: "50%", width: "15px", height: "15px", bgcolor: "#556cd6" }}>
                                                                </Box>
                                                            )
                                                        }
                                                    </Box>
                                                </Link>
                                            )
                                        })
                                    }
                                    <Box textAlign="center">
                                        <Button onClick={handleGetNotification}>Xem thêm</Button>
                                    </Box>
                                </Paper>
                            )
                        }
                    </Box>
                    <Avatar alt='Khai Dao' src={tutorInfo?.imageUrl ? tutorInfo.imageUrl : '/'} sx={{
                        bgcolor: deepPurple[500], width: "30px",
                        height: "30px",
                        cursor: "pointer"
                    }} onClick={handleOpenAccountMenu} />

                    <Menu
                        anchorEl={accountMenu}
                        id="account-menu"
                        open={openAccountMenu}
                        onClose={handleCloseAccountMenu}
                        onClick={handleCloseAccountMenu}
                        slotProps={{
                            paper: {
                                elevation: 0,
                                sx: {
                                    overflow: 'visible',
                                    filter: 'drop-shadow(0px 2px 8px rgba(0,0,0,0.32))',
                                    mt: 1.5,
                                    '& .MuiAvatar-root': {
                                        width: 32,
                                        height: 32,
                                        ml: -0.5,
                                        mr: 1
                                    },
                                    '&::before': {
                                        content: '""',
                                        display: 'block',
                                        position: 'absolute',
                                        top: 0,
                                        right: 14,
                                        width: 10,
                                        height: 10,
                                        bgcolor: 'background.paper',
                                        transform: 'translateY(-50%) rotate(45deg)',
                                        zIndex: 0
                                    }
                                }
                            }
                        }}
                        transformOrigin={{ horizontal: 'right', vertical: 'top' }}
                        anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
                    >
                        <MenuItem onClick={() => nav(PAGES.CHANGE_PASSWORD_TUTOR)}>
                            <ListItemIcon>
                                <Settings fontSize="small" />
                            </ListItemIcon>
                            Đổi mật khẩu
                        </MenuItem>
                        <MenuItem onClick={() => nav(PAGES.PAYMENT_HISTORY_TUTOR)}>
                            <ListItemIcon>
                                <CurrencyExchangeIcon fontSize="small" />
                            </ListItemIcon>
                            Lịch sử giao dịch
                        </MenuItem>
                        <MenuItem onClick={handleLogout}>
                            <ListItemIcon>
                                <Logout fontSize="small" />
                            </ListItemIcon>
                            Logout
                        </MenuItem>
                    </Menu>
                </Box>
            </Stack >
            <Divider />
        </Box >
    )
}

export default TutorHeader
