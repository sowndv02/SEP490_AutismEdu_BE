import ArrowBackIosIcon from '@mui/icons-material/ArrowBackIos';
import ArrowForwardIosIcon from '@mui/icons-material/ArrowForwardIos';
import DeleteIcon from '@mui/icons-material/Delete';
import HighlightOffIcon from '@mui/icons-material/HighlightOff';
import RemoveRedEyeIcon from '@mui/icons-material/RemoveRedEye';
import { Avatar, Box, Button, Grid, IconButton, Modal, Paper, Stack, Typography } from '@mui/material';
import { enqueueSnackbar } from 'notistack';
import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import services from '~/plugins/services';
import PAGES from '~/utils/pages';
import StatusChangeConfirm from './StatusChangeConfirm';

function ReportDetail() {
    const [report, setReport] = useState(null);
    const { id } = useParams();
    const [openImage, setOpenImage] = useState(false);
    const [currentImage, setCurrentImage] = useState(0);
    const [relatedReport, setRelatedReport] = useState([]);
    const [open, setOpen] = useState(false);
    const [status, setStatus] = useState(1);

    useEffect(() => {
        const handleGetReport = async () => {
            try {
                await services.ReportManagementAPI.getReportDetail(id, (res) => {
                    setReport(res.result.result);
                    setRelatedReport(res.result.reportsRelated);
                }, (err) => {
                    console.log(err);
                })
            } catch (error) {
                console.log(error);
            }
        }

        handleGetReport();
    }, [])
    const formatDate = (date) => {
        if (!date) return "";
        const d = new Date(date);
        return `${d.getDate()}/${d.getMonth() + 1}/${d.getFullYear()}`
    }

    const getStatus = (status) => {
        let statusString = "";
        switch (status) {
            case 0:
                statusString = "Đã từ chối";
                break;
            case 1:
                statusString = "Đã tiếp nhận";
                break;
            case 2:
                statusString = "Đang chờ";
                break;
        }
        return statusString;
    }
    const formatAddress = (address) => {
        if (!address) return "";
        const addressParts = address?.split('|');
        const formattedAddress = `${addressParts[3]} - ${addressParts[2]} - ${addressParts[1]} - ${addressParts[0]}`;
        return formattedAddress;
    }
    return (
        <Stack sx={{ gap: 2, alignItems: "flex-start" }} direction='row'>
            <Box sx={{ width: "60%" }}>
                <Paper variant='elevation' sx={{ p: 3, borderRadius: 2, boxShadow: "0px 4px 12px rgba(0, 0, 0, 0.1)" }}>
                    <Typography variant='h5' sx={{ fontWeight: 600, color: "#3c4ff4" }}>Thông tin đơn tố cáo</Typography>
                    <Stack direction='row' justifyContent="space-between" sx={{ mt: 2 }}>
                        <Box>
                            <Typography>
                                <span style={{ fontWeight: "bold" }}>Ngày tạo:</span> {formatDate(report?.createdDate)}
                            </Typography>
                            <Typography>
                                <span style={{ fontWeight: "bold" }}>Người tố cáo:</span>
                                <a href={'/admin/parent-profile/' + report?.reporter?.id} style={{ color: "#3c4ff4", textDecoration: "underline" }} rel="noopener noreferrer" target="_blank">
                                    {report?.reporter?.email}
                                </a>
                            </Typography>
                        </Box>
                        <Typography>
                            <span style={{ fontWeight: "bold" }}>Trạng thái đơn:</span>
                            <span style={{ color: report?.status === 1 ? "#4caf50" : report?.status === 2 ? "#3c4ff4" : "#f44336" }}>
                                {getStatus(report?.status)}
                            </span>
                        </Typography>
                    </Stack>
                    <Typography variant='h6' sx={{ textAlign: "center", mt: 3, fontWeight: "bold" }}>{report?.title}</Typography>
                    <Typography sx={{ whiteSpace: "break-spaces", px: 2, mt: 2, color: "#555" }}>{report?.description}</Typography>
                    <Typography mt={2} fontWeight="bold" color="#b660ec">Hình ảnh bằng chứng</Typography>
                    <Stack direction='row' gap={3} mt={2}>
                        {report?.reportMedias && report?.reportMedias.length !== 0 && report?.reportMedias.map((image, index) => (
                            <Box key={index} sx={{
                                backgroundImage: `url(${image.urlMedia})`,
                                backgroundSize: 'cover',
                                backgroundPosition: 'center',
                                width: "100px",
                                height: "100px",
                                cursor: "pointer",
                                borderRadius: 2,
                                boxShadow: "0px 2px 8px rgba(0, 0, 0, 0.2)",
                                "&:hover .hoverContent": {
                                    display: "flex",
                                    alignItems: "center",
                                    justifyContent: "center",
                                    bgcolor: "rgba(69, 137, 196, 0.75)",
                                    borderRadius: 2
                                }
                            }}>
                                <Box className="hoverContent" sx={{
                                    width: "100%",
                                    height: "100%",
                                    display: "none",
                                    transition: "0.3s ease"
                                }}>
                                    <IconButton onClick={() => { setOpenImage(true); setCurrentImage(index) }} >
                                        <RemoveRedEyeIcon sx={{ color: "#fff" }} />
                                    </IconButton>
                                </Box>
                            </Box>
                        ))}
                        {report?.reportMedias[currentImage] && report?.reportMedias.length !== 0 && openImage && (
                            <Modal open={openImage} onClose={() => setOpenImage(false)}>
                                <Box display="flex" justifyContent="center" alignItems="center" height="100vh" bgcolor="rgba(0, 0, 0, 0.8)" position="relative">
                                    <img src={report?.reportMedias[currentImage]?.urlMedia} alt="large" style={{ maxWidth: '90%', maxHeight: '90%' }} />
                                    <IconButton onClick={() => setOpenImage(false)} style={{ position: 'absolute', top: 20, right: 20, color: 'white' }}>
                                        <HighlightOffIcon />
                                    </IconButton>
                                    <IconButton style={{ position: 'absolute', left: 20, color: 'white' }}
                                        onClick={() => setCurrentImage(currentImage === 0 ? 0 : currentImage - 1)}>
                                        <ArrowBackIosIcon />
                                    </IconButton>
                                    <IconButton style={{ position: 'absolute', right: 20, color: 'white' }}
                                        onClick={() => setCurrentImage(currentImage === report?.reportMedias[currentImage].length - 1 ? currentImage : currentImage + 1)}>
                                        <ArrowForwardIosIcon />
                                    </IconButton>
                                </Box>
                            </Modal>
                        )}
                    </Stack>
                    {report?.status === 2 && (
                        <Box mt={3} display="flex" justifyContent="flex-start">
                            <Button variant='contained' color='success' sx={{ mr: 2, textTransform: "none", bgcolor: "#4caf50" }}
                                onClick={() => { setOpen(true); setStatus(1) }}>Tiếp nhận</Button>
                            <Button variant='contained' color='warning' sx={{ textTransform: "none", bgcolor: "#ff9800" }}
                                onClick={() => { setOpen(true); setStatus(0) }}>Từ chối</Button>
                        </Box>
                    )}
                </Paper>
                {relatedReport.length !== 0 && (
                    <Paper variant='elevation' sx={{ p: 3, mt: 3, borderRadius: 2, boxShadow: "0px 4px 12px rgba(0, 0, 0, 0.1)" }}>
                        <Typography variant='h5' sx={{ fontWeight: 600, color: "#3c4ff4" }}>Đơn tố cáo liên quan</Typography>
                        {relatedReport.length === 0 ? (
                            <Typography>Không có đơn tố cáo liên quan nào!</Typography>
                        ) : (
                            <ul>
                                {relatedReport.map((r) => (
                                    <li key={r.id}>
                                        <Link to={PAGES.REPORT_TUTOR_MANAGEMENT + "/detail/" + r.id} style={{ textDecoration: 'underline', color: "#3c4ff4" }}
                                            onClick={(e) => { e.preventDefault(); window.open(PAGES.REPORT_TUTOR_MANAGEMENT + "/detail/" + r.id, '_blank'); }}>
                                            {r.title}
                                        </Link>
                                    </li>
                                ))}
                            </ul>
                        )}
                    </Paper>
                )}
            </Box>
            <Box sx={{ width: "40%" }}>
                {report && (
                    <Paper variant='elevation' sx={{ p: 3, borderRadius: 2, boxShadow: "0px 4px 12px rgba(0, 0, 0, 0.1)" }}>
                        <Typography variant='h5' sx={{ fontWeight: 600, color: "#3c4ff4" }}>Gia sư bị tố cáo</Typography>
                        <Avatar alt="Tutor" src={report.tutor?.imageUrl} sx={{
                            width: "150px",
                            height: "150px",
                            margin: "auto",
                            mt: 2,
                            border: "3px solid #b660ec"
                        }} />
                        <Grid container pl={2} py="50px" columnSpacing={2} rowSpacing={1.5}>
                            <Grid item xs={3} textAlign="right" sx={{ color: "#555", fontWeight: 500 }}>Họ và tên:</Grid>
                            <Grid item xs={9}>{report.tutor?.fullName}</Grid>
                            <Grid item xs={3} textAlign="right" sx={{ color: "#555", fontWeight: 500 }}>Email:</Grid>
                            <Grid item xs={9}>{report.tutor?.email}</Grid>
                            <Grid item xs={3} textAlign="right" sx={{ color: "#555", fontWeight: 500 }}>Ngày sinh:</Grid>
                            <Grid item xs={9}>{formatDate(report.tutor?.dateOfBirth)}</Grid>
                            <Grid item xs={3} textAlign="right" sx={{ color: "#555", fontWeight: 500 }}>Địa chỉ:</Grid>
                            <Grid item xs={9}>{formatAddress(report.tutor?.address)}</Grid>
                            <Grid item xs={3} textAlign="right" sx={{ color: "#555", fontWeight: 500 }}>Số điện thoại:</Grid>
                            <Grid item xs={9}>{report.tutor?.phoneNumber}</Grid>
                        </Grid>
                        <a href={'/admin/tutor-profile/' + report?.tutor?.userId} rel="noopener noreferrer" target="_blank">
                            <Button variant='contained' sx={{ bgcolor: "#3c4ff4", color: "#fff", mt: 2 }}>Xem chi tiết</Button>
                        </a>
                    </Paper>
                )}
            </Box>
            <StatusChangeConfirm status={status} id={report?.id || 0} open={open} setOpen={setOpen} setReport={setReport} report={report} />
        </Stack>
    )
}

export default ReportDetail
