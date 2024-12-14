import AssessmentIcon from '@mui/icons-material/Assessment';
import AssignmentIcon from '@mui/icons-material/Assignment';
import AttachMoneyIcon from '@mui/icons-material/AttachMoney';
import AutoStoriesIcon from '@mui/icons-material/AutoStories';
import DashboardIcon from '@mui/icons-material/Dashboard';
import DescriptionIcon from '@mui/icons-material/Description';
import ExpandLess from '@mui/icons-material/ExpandLess';
import ExpandMore from '@mui/icons-material/ExpandMore';
import FeedIcon from '@mui/icons-material/Feed';
import ManageAccountsIcon from '@mui/icons-material/ManageAccounts';
import MenuBookIcon from '@mui/icons-material/MenuBook';
import NewspaperIcon from '@mui/icons-material/Newspaper';
import PeopleIcon from '@mui/icons-material/People';
import PermContactCalendarIcon from '@mui/icons-material/PermContactCalendar';
import PlaylistAddIcon from '@mui/icons-material/PlaylistAdd';
import PointOfSaleIcon from '@mui/icons-material/PointOfSale';
import PostAddIcon from '@mui/icons-material/PostAdd';
import ReportIcon from '@mui/icons-material/Report';
import SortIcon from '@mui/icons-material/Sort';
import TocIcon from '@mui/icons-material/Toc';
import WorkIcon from '@mui/icons-material/Work';
import AdminPanelSettingsIcon from '@mui/icons-material/AdminPanelSettings';
import CurrencyExchangeIcon from '@mui/icons-material/CurrencyExchange';
import { Box, Collapse, List, ListItemButton, ListItemIcon, ListItemText } from '@mui/material';
import { useEffect, useState } from 'react';
import { useSelector } from 'react-redux';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { adminInfor } from '~/redux/features/adminSlice';
import PAGES from '~/utils/pages';

function AdminLeftBar() {
    const [open, setOpen] = useState(false);
    const [openPayment, setOpenPayment] = useState(false);
    const [openArtical, setOpenArtical] = useState(false);
    const [openInformation, setOpenInformation] = useState(false);
    const [openReport, setOpenReport] = useState(false);
    const location = useLocation();
    const nav = useNavigate();
    const adminInformation = useSelector(adminInfor);
    const handleClick = () => {
        setOpen(!open);
    };
    const [selectedIndex, setSelectedIndex] = useState(0);

    useEffect(() => {
        if (location.pathname.includes("/dashboard")) {
            setSelectedIndex(0);
        } else if (location.pathname.includes("/user-management")) {
            setSelectedIndex(1);
        } else if (location.pathname.includes("/admin/parent-tutor-management")) {
            setSelectedIndex(1);
        }
        else if (location.pathname.includes("/role-claim-management")) {
            setSelectedIndex(2);
        }
        else if (location.pathname.includes("/tutor-registration-management")) {
            setSelectedIndex(3);
        }
        else if (location.pathname.includes("/exercise-type-management")) {
            setSelectedIndex(4);
        } else if (location.pathname.includes("/assessment-management")) {
            setSelectedIndex(5);
        }
        else if (location.pathname.includes("/assessment-creation")) {
            setSelectedIndex(6);
        }
        else if (location.pathname.includes("/assessment_score_range")) {
            setSelectedIndex(7);
        }
        else if (location.pathname.includes("/payment-package-management")) {
            setSelectedIndex(8);
        }
        else if (location.pathname.includes("/payment-history")) {
            setSelectedIndex(9);
        }
        else if (location.pathname.includes("/blog-management")) {
            setSelectedIndex(10);
        }
        else if (location.pathname.includes("/blog-creation")) {
            setSelectedIndex(11);
        }
        else if (location.pathname.includes("/report-tutor-management")) {
            setSelectedIndex(12);
        }
        else if (location.pathname.includes("/personal-information")) {
            setSelectedIndex(14);
        }
        else if (location.pathname.includes("/curriculum-management")) {
            setSelectedIndex(15);
        }
        else if (location.pathname.includes("/certificate-management")) {
            setSelectedIndex(16);
        }
        else if (location.pathname.includes("/work-experience-management")) {
            setSelectedIndex(17);
        }
    }, [location])

    // useEffect(() => {
    //     if (adminInformation?.role === 'Admin') {
    //         nav(PAGES.USERMANAGEMENT);
    //         setSelectedIndex(1);
    //     } else if (adminInformation?.role === 'Staff') {
    //         nav(PAGES.PARENT_TUTOR_MAMAGEMENT);
    //         setSelectedIndex(1);
    //     }
    // }, [adminInformation])
    const handleListItemClick = (event, index) => {
        setSelectedIndex(index);
    };

    return (
        <>
            <Box sx={{ bgcolor: "white", height: "100%", px: "15px", pt: "20px" }}>
                <List
                    sx={{ width: '100%' }}
                    component="nav"
                    aria-labelledby="nested-list-subheader"
                >
                    {
                        adminInformation?.role === "Manager" && (
                            <Link to="/admin/dashboard">
                                <ListItemButton
                                    selected={selectedIndex === 0}
                                    onClick={(event) => handleListItemClick(event, 0)}>
                                    <ListItemIcon>
                                        <DashboardIcon />
                                    </ListItemIcon>
                                    <ListItemText primary="Dashboard" />
                                </ListItemButton>
                            </Link>
                        )
                    }
                    {
                        adminInformation?.role === "Admin" && (
                            <Link to="/admin/user-management">
                                <ListItemButton
                                    selected={selectedIndex === 1}
                                    onClick={(event) => handleListItemClick(event, 1)}>
                                    <ListItemIcon>
                                        <PeopleIcon />
                                    </ListItemIcon>
                                    <ListItemText primary="Tài Khoản" />
                                </ListItemButton>
                            </Link>
                        )
                    }
                    {
                        (adminInformation?.role === "Manager" || adminInformation?.role === "Staff") && (
                            <Link to="/admin/parent-tutor-management">
                                <ListItemButton
                                    selected={selectedIndex === 1}
                                    onClick={(event) => handleListItemClick(event, 1)}>
                                    <ListItemIcon>
                                        <PeopleIcon />
                                    </ListItemIcon>
                                    <ListItemText primary="Tài Khoản" />
                                </ListItemButton>
                            </Link>
                        )
                    }
                    {
                        adminInformation?.role === "Admin" && (
                            <Link to="/admin/role-claim-management">
                                <ListItemButton
                                    selected={selectedIndex === 2}
                                    onClick={(event) => handleListItemClick(event, 2)}>
                                    <ListItemIcon>
                                        <AdminPanelSettingsIcon />
                                    </ListItemIcon>
                                    <ListItemText primary="Vai trò" />
                                </ListItemButton>
                            </Link>
                        )}
                    {
                        (adminInformation?.role === "Manager" || adminInformation?.role === "Staff") && (
                            <Link to={PAGES.TUTORREGISTRATIONMANAGEMENT}>
                                <ListItemButton
                                    selected={selectedIndex === 3}
                                    onClick={(event) => handleListItemClick(event, 3)}>
                                    <ListItemIcon>
                                        <FeedIcon />
                                    </ListItemIcon>
                                    <ListItemText primary="Đơn Đăng Ký" />
                                </ListItemButton>
                            </Link>
                        )}
                    {
                        (adminInformation?.role === "Manager" || adminInformation?.role === "Staff") && (
                            <Link to="/admin/exercise-type-management">
                                <ListItemButton
                                    selected={selectedIndex === 4}
                                    onClick={(event) => handleListItemClick(event, 4)}>
                                    <ListItemIcon>
                                        <MenuBookIcon />
                                    </ListItemIcon>
                                    <ListItemText primary="Loại bài tập" />
                                </ListItemButton>
                            </Link>
                        )}
                    {
                        (adminInformation?.role === "Manager" || adminInformation?.role === "Staff") && (
                            <>
                                <ListItemButton onClick={handleClick}>
                                    <ListItemIcon>
                                        <AssessmentIcon />
                                    </ListItemIcon>
                                    <ListItemText primary="Đánh giá" />
                                    {open ? <ExpandLess /> : <ExpandMore />}
                                </ListItemButton>

                                <Collapse in={open} timeout="auto" unmountOnExit>
                                    <Link to={PAGES.ASSESSMENT_MANAGEMENT}>
                                        <List component="div" disablePadding>
                                            <ListItemButton sx={{ pl: 4 }}
                                                selected={selectedIndex === 5}
                                                onClick={(event) => handleListItemClick(event, 5)}>
                                                <ListItemIcon>
                                                    <TocIcon />
                                                </ListItemIcon>
                                                <ListItemText primary="Danh sách" />
                                            </ListItemButton>
                                        </List>
                                    </Link>
                                    <Link to={PAGES.ASSESSMENT_CREATION}>
                                        <List component="div" disablePadding>
                                            <ListItemButton sx={{ pl: 4 }}
                                                selected={selectedIndex === 6}
                                                onClick={(event) => handleListItemClick(event, 6)}>
                                                <ListItemIcon>
                                                    <PlaylistAddIcon />
                                                </ListItemIcon>
                                                <ListItemText primary="Thêm đánh giá" />
                                            </ListItemButton>
                                        </List>
                                    </Link>

                                    <Link to={PAGES.SCORE_RANGE}>
                                        <List component="div" disablePadding>
                                            <ListItemButton sx={{ pl: 4 }}
                                                selected={selectedIndex === 7}
                                                onClick={(event) => handleListItemClick(event, 7)}>
                                                <ListItemIcon>
                                                    <AssignmentIcon />
                                                </ListItemIcon>
                                                <ListItemText primary="Đánh giá chung" />
                                            </ListItemButton>
                                        </List>
                                    </Link>
                                </Collapse>
                            </>
                        )}
                    {
                        adminInformation?.role === "Manager" && (
                            <>
                                <ListItemButton onClick={() => setOpenPayment(!openPayment)}>
                                    <ListItemIcon>
                                        <AttachMoneyIcon />
                                    </ListItemIcon>
                                    <ListItemText primary="Thanh Toán" />
                                    {openPayment ? <ExpandLess /> : <ExpandMore />}
                                </ListItemButton>
                                <Collapse
                                    in={openPayment} timeout="auto" unmountOnExit>
                                    <Link to={PAGES.PAYMENT_PACKAGE_MANAGEMENT}>
                                        <List component="div" disablePadding>
                                            <ListItemButton sx={{ pl: 4 }}
                                                selected={selectedIndex === 8}
                                                onClick={(event) => handleListItemClick(event, 8)}>
                                                <ListItemIcon>
                                                    <PointOfSaleIcon />
                                                </ListItemIcon>
                                                <ListItemText primary="Gói Thanh Toán" />
                                            </ListItemButton>
                                        </List>
                                    </Link>
                                    <Link to={PAGES.PAYMENT_HISTORY_ADMIN}>
                                        <List component="div" disablePadding>
                                            <ListItemButton sx={{ pl: 4 }}
                                                selected={selectedIndex === 9}
                                                onClick={(event) => handleListItemClick(event, 9)}>
                                                <ListItemIcon>
                                                    <CurrencyExchangeIcon />
                                                </ListItemIcon>
                                                <ListItemText primary="Lịch sử thanh toán" />
                                            </ListItemButton>
                                        </List>
                                    </Link>
                                </Collapse>
                            </>
                        )}

                    {
                        (adminInformation?.role === "Manager" || adminInformation?.role === "Staff") && (
                            <>
                                <ListItemButton onClick={() => setOpenArtical(!openArtical)}>
                                    <ListItemIcon>
                                        <NewspaperIcon />
                                    </ListItemIcon>
                                    <ListItemText primary="Bài viết" />
                                    {openArtical ? <ExpandLess /> : <ExpandMore />}
                                </ListItemButton>
                                <Collapse in={openArtical} timeout="auto" unmountOnExit>
                                    <Link to={PAGES.BLOG_MANAGEMENT}>
                                        <List component="div" disablePadding>
                                            <ListItemButton sx={{ pl: 4 }}
                                                selected={selectedIndex === 10}
                                                onClick={(event) => handleListItemClick(event, 10)}>
                                                <ListItemIcon>
                                                    <SortIcon />
                                                </ListItemIcon>
                                                <ListItemText primary="Danh sách bài viết" />
                                            </ListItemButton>
                                        </List>
                                    </Link>
                                    <Link to={PAGES.BLOG_CREATION}>
                                        <List component="div" disablePadding>
                                            <ListItemButton sx={{ pl: 4 }}
                                                selected={selectedIndex === 11}
                                                onClick={(event) => handleListItemClick(event, 11)}>
                                                <ListItemIcon>
                                                    <PostAddIcon />
                                                </ListItemIcon>
                                                <ListItemText primary="Tạo bài viết" />
                                            </ListItemButton>
                                        </List>
                                    </Link>
                                </Collapse>
                            </>
                        )}

                    {
                        (adminInformation?.role === "Manager" || adminInformation?.role === "Staff") && (
                            <>
                                <ListItemButton onClick={() => setOpenReport(!openReport)}>
                                    <ListItemIcon>
                                        <ReportIcon />
                                    </ListItemIcon>
                                    <ListItemText primary="Đơn tố cáo" />
                                    {openReport ? <ExpandLess /> : <ExpandMore />}
                                </ListItemButton>
                                <Collapse in={openReport} timeout="auto" unmountOnExit>
                                    <Link to={PAGES.REPORT_TUTOR_MANAGEMENT}>
                                        <List component="div" disablePadding>
                                            <ListItemButton sx={{ pl: 4 }}
                                                selected={selectedIndex === 12}
                                                onClick={(event) => handleListItemClick(event, 12)}>
                                                <ListItemText primary="Tố cáo gia sư" />
                                            </ListItemButton>
                                        </List>
                                    </Link>
                                    <Link to={PAGES.ADMIN_REPORT_REVIEW}>
                                        <List component="div" disablePadding>
                                            <ListItemButton sx={{ pl: 4 }}
                                                selected={selectedIndex === 13}
                                                onClick={(event) => handleListItemClick(event, 13)}>
                                                <ListItemText primary="Tố cáo đánh giá" />
                                            </ListItemButton>
                                        </List>
                                    </Link>
                                </Collapse>
                            </>)}
                    {
                        (adminInformation?.role === "Manager" || adminInformation?.role === "Staff") && (
                            <>
                                <ListItemButton onClick={() => setOpenInformation(!openInformation)}>
                                    <ListItemIcon>
                                        <PermContactCalendarIcon />
                                    </ListItemIcon>
                                    <ListItemText primary="Thông tin gia sư" />
                                    {openInformation ? <ExpandLess /> : <ExpandMore />}
                                </ListItemButton>
                                <Collapse in={openInformation} timeout="auto" unmountOnExit>
                                    <Link to={PAGES.PERSONAL_INFORMATION}>
                                        <List component="div" disablePadding>
                                            <ListItemButton sx={{ pl: 4 }}
                                                selected={selectedIndex === 14}
                                                onClick={(event) => handleListItemClick(event, 14)}>
                                                <ListItemIcon>
                                                    <ManageAccountsIcon />
                                                </ListItemIcon>
                                                <ListItemText primary="Cập nhật thông tin" />
                                            </ListItemButton>
                                        </List>
                                    </Link>
                                    <Link to={PAGES.CURRICULUM_MANAGEMENT}>
                                        <List component="div" disablePadding>
                                            <ListItemButton sx={{ pl: 4 }}
                                                selected={selectedIndex === 15}
                                                onClick={(event) => handleListItemClick(event, 15)}>
                                                <ListItemIcon>
                                                    <AutoStoriesIcon />
                                                </ListItemIcon>
                                                <ListItemText primary="Khung chương trình" />
                                            </ListItemButton>
                                        </List>
                                    </Link>
                                    <Link to={PAGES.CERTIFICATE_MANAGEMENT}>
                                        <List component="div" disablePadding>
                                            <ListItemButton sx={{ pl: 4 }}
                                                selected={selectedIndex === 16}
                                                onClick={(event) => handleListItemClick(event, 16)}>
                                                <ListItemIcon>
                                                    <DescriptionIcon />
                                                </ListItemIcon>
                                                <ListItemText primary="Chứng chỉ" />
                                            </ListItemButton>
                                        </List>
                                    </Link>
                                    <Link to={PAGES.WORK_EXPERIENCE_MANAGEMENT}>
                                        <List component="div" disablePadding>
                                            <ListItemButton sx={{ pl: 4 }}
                                                selected={selectedIndex === 17}
                                                onClick={(event) => handleListItemClick(event, 17)}>
                                                <ListItemIcon>
                                                    <WorkIcon />
                                                </ListItemIcon>
                                                <ListItemText primary="Kinh nghiệm làm việc" />
                                            </ListItemButton>
                                        </List>
                                    </Link>
                                </Collapse>
                            </>
                        )}
                </List>
            </Box>
        </>
    )
}

export default AdminLeftBar
