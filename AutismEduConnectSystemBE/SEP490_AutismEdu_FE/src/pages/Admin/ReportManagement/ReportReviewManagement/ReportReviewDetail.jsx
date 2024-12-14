import { Avatar, Box, Button, Grid, Paper, Rating, Stack, Typography } from '@mui/material';
import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import services from '~/plugins/services';
import StatusChangeConfirm from '../ReportTutorManagement/StatusChangeConfirm';

function ReportReviewDetail() {
    const [report, setReport] = useState(null);
    const { id } = useParams();
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
    console.log(report);
    return (
        <Stack sx={{ gap: 2, alignItems: "flex-start" }} direction='row'>
            <Box sx={{
                width: "60%"
            }}>
                <Paper variant='elevation' sx={{
                    p: 2
                }}>
                    <Typography variant='h5'>Thông tin đơn tố cáo</Typography>
                    <Stack direction='row' justifyContent="space-between" sx={{ mt: 2 }}>
                        <Box>
                            <Typography><span style={{ fontWeight: "bold" }}>Ngày tạo:</span> {formatDate(report?.createdDate)}</Typography>
                            <Typography><span style={{ fontWeight: "bold", marginRight: "10px" }}>Người tố cáo:</span>
                                <a href={'/admin/parent-profile/' + report?.reporter?.id} style={{ color: "blue", textDecoration: "underline" }}
                                    rel="noopener noreferrer" target="_blank"
                                >
                                    {report?.reporter?.email}
                                </a></Typography>
                        </Box>
                        <Typography>
                            <span style={{ fontWeight: "bold", marginRight: "10px" }}>Trạng thái đơn:</span>
                            <span style={{ color: report?.status === 1 ? "green" : report?.status === 2 ? "blue" : "red" }}>{getStatus(report?.status)}</span>
                        </Typography>
                    </Stack>
                    <Stack direction='row' gap={2} mt={3}>
                        <Typography fontWeight="bold">Nội dung đánh giá:</Typography>
                        <Box>
                            <Rating name="read-only" value={report?.review?.rateScore || 0} readOnly />
                            <Typography sx={{ whiteSpace: "break-spaces" }}>{report?.review?.description}</Typography>
                        </Box>
                    </Stack>
                    <Stack direction='row' gap={2} mt={3}>
                        <Typography fontWeight="bold" sx={{ width: "20%" }}>Lý do tố cáo:</Typography>
                        <Typography sx={{ whiteSpace: "break-spaces", width: "70%" }}>{report?.description}</Typography>
                    </Stack>
                    {
                        report?.status === 2 && (
                            <Box mt={2}>
                                <Button variant='contained' color='success' sx={{ mr: 2 }}
                                    onClick={() => { setOpen(true); setStatus(1) }}
                                >Tiếp nhận</Button>
                                <Button variant='contained' color='warning'
                                    onClick={() => { setOpen(true); setStatus(0) }}
                                >Từ chối</Button>
                            </Box>
                        )
                    }
                </Paper>
                {
                    relatedReport.length !== 0 && (
                        <Paper variant='elevation' sx={{ p: 2, mt: 2 }}>
                            <Typography variant='h5'>Đơn tố cáo liên quan</Typography>
                            {
                                relatedReport.length === 0 && (
                                    <Typography>Không có đơn tố cáo liên quan nào!</Typography>
                                )
                            }
                            {
                                relatedReport.length !== 0 && (
                                    <ul>
                                        {
                                            relatedReport.map((r) => {
                                                return (
                                                    <li key={r.id}>
                                                        <Link to={'/admin/report-review-management/' + r.id} style={{ textDecoration: 'underline', color: "blue" }}
                                                            onClick={(e) => {
                                                                e.preventDefault();
                                                                window.open('/admin/report-review-management/' + r.id, '_blank');
                                                            }}>
                                                            {r?.reporter.email}
                                                        </Link>
                                                    </li>
                                                )
                                            })
                                        }
                                    </ul>
                                )
                            }
                        </Paper>
                    )
                }
            </Box>
            <Box sx={{ width: "40%" }}>
                {
                    report && (
                        <Paper variant='elevation' sx={{ p: 2 }}>
                            <Typography variant='h5'>Người đăng đánh giá</Typography>
                            <Avatar alt="Remy Sharp" src={report?.review?.parent?.imageUrl} sx={{
                                width: "150px",
                                height: "150px",
                                margin: "auto",
                                mt: 2
                            }} />
                            <Grid container pl={2} py="50px" columnSpacing={2} rowSpacing={1.5}>
                                <Grid item xs={3} textAlign="right">Họ và tên:</Grid>
                                <Grid item xs={9}>{report?.review?.parent?.fullName}</Grid>
                                <Grid item xs={3} textAlign="right">Email:</Grid>
                                <Grid item xs={9}>{report?.review?.parent?.email}</Grid>
                                <Grid item xs={3} textAlign="right">Địa chỉ:</Grid>
                                <Grid item xs={9}>{formatAddress(report?.review?.parent?.address)}</Grid>
                                <Grid item xs={3} textAlign="right">Số điện thoại:</Grid>
                                <Grid item xs={9}>{report?.review?.parent?.phoneNumber}</Grid>
                            </Grid>
                            <a href={'/admin/parent-profile/' + report?.review?.parent?.id} rel="noopener noreferrer" target="_blank">
                                <Button>Xem chi tiết</Button>
                            </a>
                        </Paper>
                    )
                }
            </Box>
            <StatusChangeConfirm status={status} id={report?.id || 0} open={open} setOpen={setOpen} setReport={setReport}
                report={report} />
        </Stack>
    )
}

export default ReportReviewDetail
